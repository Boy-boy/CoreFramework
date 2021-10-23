using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Translate
{
    public interface ITranslateProvider
    {
        /// <summary>
        /// 翻译转换
        /// </summary>
        /// <param name="original">原始数据</param>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>若出现错误，将以original输出</returns>
        Task<List<string>> TranslateAsync(string original,
           TranslateQueryDto query,
           CancellationToken cancellationToken = default);
    }
}
