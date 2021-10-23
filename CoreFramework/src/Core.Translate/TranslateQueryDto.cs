namespace Core.Translate
{
    public class TranslateQueryDto
    {
        public static TranslateQueryDto Default = new();

        /// <summary>
        /// 翻译源语言
        /// </summary>
        public string From { get; set; } = "zh";

        /// <summary>
        /// 翻译目标语言
        /// </summary>
        public string To { get; set; } = "en";
    }
}
