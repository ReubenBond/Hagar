using System;

namespace FakeFx.Runtime
{
    [Serializable]
    [Hagar.GenerateSerializer]
    [Hagar.WellKnownId(103)]
    [Hagar.SuppressReferenceTracking]
    internal sealed class Response
    {
        [Hagar.Id(1)]
        public bool ExceptionFlag { get; private set; }

        [Hagar.Id(2)]
        public Exception Exception { get; private set; }

        [Hagar.Id(3)]
        public object Data { get; private set; }

        public Response(object data)
        {
            switch (data)
            {
                case Exception exception:
                    Exception = exception;
                    ExceptionFlag = true;
                    break;
                default:
                    Data = data;
                    ExceptionFlag = false;
                    break;
            }
        }

        private Response()
        {
        }

        static public Response ExceptionResponse(Exception exc)
        {
            return new Response
            {
                ExceptionFlag = true,
                Exception = exc
            };
        }

        public override string ToString()
        {
            if (ExceptionFlag)
            {
                return $"Response Exception={Exception}";
            }

            return $"Response Data={Data}";
        }
    }
}
