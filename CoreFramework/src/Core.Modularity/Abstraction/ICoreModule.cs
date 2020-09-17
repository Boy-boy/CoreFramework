namespace Core.Modularity.Abstraction
{
    public interface ICoreModule
    {
        void PreConfigureServices(ServiceCollectionContext context);
        void ConfigureServices(ServiceCollectionContext context);
        void PostConfigureServices(ServiceCollectionContext context);


        void PreConfigure(ConfigureApplicationContext context);
        void Configure(ConfigureApplicationContext app);

        void PostConfigure(ConfigureApplicationContext context);

        void Shutdown(ShutdownApplicationContext context);
    }
}
