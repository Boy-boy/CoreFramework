namespace Core.Pipeline
{
    public interface IRequest
    {
        public IEnumerable<KeyValuePair<Type, object>> Features { get; set; }
    }
}
