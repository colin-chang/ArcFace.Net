namespace ColinChang.ArcFace.Models
{
    public class OperationResult<T>
    {
        public OperationResult(T data, long code = 0)
        {
            Data = data;
            Code = code;
        }

        public OperationResult(long code) : this(default, code)
        {
        }

        public T Data { get; set; }
        public long Code { get; set; }

        public OperationResult<TK> Cast<TK>()
        {
            if (Data is ICast<TK> cast)
                return new OperationResult<TK>(cast.Cast(), Code);

            return this as OperationResult<TK>;
        }
    }
}