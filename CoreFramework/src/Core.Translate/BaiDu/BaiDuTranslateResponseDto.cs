using System.Collections.Generic;

namespace Core.Translate.BaiDu
{
    public class BaiDuTranslateResponseDto
    {
        /// <summary>
        /// 源语言
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// 目标语言
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// 翻译结果
        /// </summary>
        public List<TransDto> trans_result { get; set; }

        /// <summary>
        /// 错误码
        /// </summary>
        public int error_code { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string error_msg { get; set; }

    }

    public class TransDto
    {
        /// <summary>
        /// 原文
        /// </summary>
        public string Src { get; set; }

        /// <summary>
        /// 译文
        /// </summary>
        public string Dst { get; set; }
    }
}
