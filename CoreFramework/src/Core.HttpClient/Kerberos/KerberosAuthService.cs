using Kerberos.NET.Client;
using Kerberos.NET.Configuration;
using Kerberos.NET.Credentials;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Core.HttpClient.Kerberos
{
    public class KerberosAuthService : IKerberosAuthService, IDisposable
    {
        private KerberosOptions _options;
        private readonly IMemoryCache _cache;
        private readonly ILogger<KerberosAuthService> _logger;
        private KerberosClient _kerberosClient;
        private KeyTable _keyTable;
        private bool _disposed;
        private readonly IDisposable _optionsChangeToken;
        private readonly ConcurrentDictionary<string, byte> _cacheKeyTracker = new();

        public KerberosAuthService(
            IOptionsMonitor<KerberosOptions> optionsMonitor,
            IMemoryCache memoryCache,
            ILogger<KerberosAuthService> logger)
        {
            _options = optionsMonitor.CurrentValue;
            _cache = memoryCache;
            _logger = logger;

            LoadKeyTable().GetAwaiter().GetResult();
            InitializeKerberosClient();

            // 注册配置变更监听
            _optionsChangeToken = optionsMonitor.OnChange(OnOptionsChanged);
        }
        private void OnOptionsChanged(KerberosOptions newOptions, string name)
        {
            try
            {
                _logger.LogInformation("Kerberos configuration changed. Reloading...");

                // 更新选项
                _options = newOptions;

                // 清除缓存
                ClearCache();

                // 重新初始化 Kerberos 客户端
                ReinitializeKerberosClient();

                _logger.LogInformation("Kerberos configuration reloaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload Kerberos configuration");
                // 注意：不要抛出异常，否则会中断配置变更监听
            }
        }

        private void ReinitializeKerberosClient()
        {
            try
            {
                // 释放旧的客户端
                _kerberosClient.Dispose();

                // 重新加载 keytab
                LoadKeyTable().GetAwaiter().GetResult();

                // 重新初始化客户端
                InitializeKerberosClient();

                _logger.LogInformation("Kerberos client reinitialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reinitialize Kerberos client");
                throw;
            }
        }

        private void ClearCache()
        {
            try
            {
                var removedCount = 0;

                // 清理所有跟踪的缓存项
                foreach (var cacheKey in _cacheKeyTracker.Keys.ToArray()) // 使用 ToArray() 避免修改时枚举
                {
                    _cache.Remove(cacheKey);
                    _cacheKeyTracker.TryRemove(cacheKey, out _);
                    removedCount++;
                }

                _logger.LogInformation("Cleared {Count} cached negotiate tokens", removedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cache");
            }
        }

        private void InitializeKerberosClient()
        {
            try
            {
                // 1. 创建 Kerberos 配置
                var config = CreateKrb5Config();
                // 2. 创建 KerberosClient（必须传入配置）
                _kerberosClient = new KerberosClient(config);

                _logger.LogInformation("Kerberos client initialized for realm: {Realm}", _options.Realm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Kerberos client");
                throw;
            }
        }

        private Krb5Config CreateKrb5Config()
        {
            var config = new Krb5Config();

            // 配置默认领域
            if (!string.IsNullOrEmpty(_options.Realm))
            {
                config.Defaults.DefaultRealm = _options.Realm;
            }

            // 配置领域特定的设置
            if (!string.IsNullOrEmpty(_options.Realm) && !string.IsNullOrEmpty(_options.Kdc))
            {
                config.Realms[_options.Realm] = new Krb5RealmConfig
                {
                    Kdc = { _options.Kdc } // 注意：这里使用数组而不是列表
                };
                _logger.LogDebug("Configured KDC {Kdc} for realm {Realm}", _options.Kdc, _options.Realm);
            }

            // 配置其他默认选项
            config.Defaults.TicketLifetime = _options.TicketLifetime;
            config.Defaults.RenewLifetime = _options.RenewLifetime;
            config.Defaults.ClockSkew = _options.ClockSkew;

            // 添加推荐的默认配置
            config.Defaults.DnsLookupKdc = true;
            config.Defaults.Forwardable = true;
            return config;
        }

        private async Task LoadKeyTable()
        {
            try
            {
                byte[] keytabBytes;

                if (!string.IsNullOrEmpty(_options.KeytabBase64))
                {
                    keytabBytes = Convert.FromBase64String(_options.KeytabBase64);
                    _logger.LogInformation("Loaded keytab from Base64 string");
                }
                else if (!string.IsNullOrEmpty(_options.KeytabPath) && File.Exists(_options.KeytabPath))
                {
                    keytabBytes = await File.ReadAllBytesAsync(_options.KeytabPath);
                    _logger.LogInformation("Loaded keytab from file: {KeytabPath}", _options.KeytabPath);
                }
                else
                {
                    throw new InvalidOperationException(
                        "No valid keytab source provided. Either KeytabBase64 or KeytabPath must be configured.");
                }

                // 使用 KeyTable 创建凭证
                _keyTable = new KeyTable(keytabBytes);

                _logger.LogInformation("Keytable loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load keytable");
                throw;
            }
        }

        public async Task<string> GetNegotiateTokenAsync(string servicePrincipalName)
        {
            if (string.IsNullOrEmpty(servicePrincipalName))
            {
                servicePrincipalName = _options.DefaultServiceSpn;
            }

            if (string.IsNullOrEmpty(servicePrincipalName))
            {
                throw new ArgumentException("Service principal name is required", nameof(servicePrincipalName));
            }

            var cacheKey = $"negotiate_token_{servicePrincipalName}";

            if (_cache.TryGetValue(cacheKey, out string cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                _logger.LogDebug("Using cached negotiate token for {ServiceSpn}", servicePrincipalName);
                return cachedToken;
            }

            try
            {
                // 获取凭证
                var credential = new KeytabCredential(_options.Principal, _keyTable);

                // 认证获取 TGT
                await _kerberosClient.Authenticate(credential);

                // 获取服务票据
                var serviceTicket = await _kerberosClient.GetServiceTicket(servicePrincipalName);

                // 编码为 GSSAPI 格式的 Negotiate token
                var gssApiToken = serviceTicket.EncodeGssApi().ToArray();
                var negotiateToken = Convert.ToBase64String(gssApiToken);


                // 创建缓存选项并注册过期回调
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(_options.TicketCacheDuration)
                    .RegisterPostEvictionCallback(OnCacheEntryEvicted!);
                // 缓存 token
                _cache.Set(cacheKey, negotiateToken, cacheOptions);
                _cacheKeyTracker[cacheKey] = 0;

                _logger.LogInformation("Successfully acquired negotiate token for {ServiceSpn}", servicePrincipalName);

                return negotiateToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get negotiate token for {ServiceSpn}", servicePrincipalName);
                throw new InvalidOperationException($"Kerberos authentication failed: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidateKerberosConfigurationAsync()
        {
            try
            {
                // 测试配置是否有效
                var credential = new KeytabCredential(_options.Principal, _keyTable);

                // 尝试认证
                await _kerberosClient.Authenticate(credential);

                _logger.LogInformation("Kerberos configuration validation successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kerberos configuration validation failed");
                return false;
            }
        }

        public async Task<KrbApReq> GetServiceTicketRawAsync(string servicePrincipalName)
        {
            // 获取原始的 KrbApReq 票据（高级用法）
            var credential = new KeytabCredential(_options.Principal, _keyTable);
            await _kerberosClient.Authenticate(credential);

            return await _kerberosClient.GetServiceTicket(servicePrincipalName);
        }

        private void OnCacheEntryEvicted(object key, object value, EvictionReason reason, object state)
        {
            if (key is string cacheKey)
            {
                _cacheKeyTracker.TryRemove(cacheKey, out _);
                _logger.LogDebug("Cache entry {Key} evicted due to {Reason}", cacheKey, reason);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _optionsChangeToken?.Dispose();
                _kerberosClient?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~KerberosAuthService()
        {
            Dispose();
        }
    }
}
