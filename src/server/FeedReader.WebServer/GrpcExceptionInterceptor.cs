using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FeedReader.Server
{
    public class GrpcExceptionInterceptor : Interceptor
    {
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await continuation(request, context);
            }
            catch (ArgumentException ex)
            {
                context.GetHttpContext().Response.StatusCode = (int)HttpStatusCode.BadRequest;
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (UnauthorizedAccessException ex)
            {
                context.GetHttpContext().Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
            }
            catch (Exception ex)
            {
                context.GetHttpContext().Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }
    }
}
