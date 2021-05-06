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

        /// <summary>
        /// 自定义数据结果
        /// </summary>
        public T Data { get; set; }
        
        /// <summary>
        /// 虹软错误码（参阅：https://github.com/colin-chang/ArcFace.Net）
        /// </summary>
        public long Code { get; set; }

        public OperationResult<TK> Cast<TK>()
        {
            if (Data is ICast<TK> cast)
                return new OperationResult<TK>(cast.Cast(), Code);

            return this as OperationResult<TK>;
        }
    }
}