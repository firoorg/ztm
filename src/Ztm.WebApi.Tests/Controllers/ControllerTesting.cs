using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ztm.WebApi.Callbacks;
using Ztm.WebApi.Controllers;

namespace Ztm.WebApi.Tests.Controllers
{
    public abstract class ControllerTesting<T> where T : ControllerBase
    {
        protected const string CallbackId = ControllerBaseExtensions.CallbackIdHeader;

        protected const string CallbackUrl = ControllerBaseExtensions.CallbackUrlHeader;

        readonly Lazy<T> subject;

        protected ControllerTesting()
        {
            this.subject = new Lazy<T>(() =>
            {
                var controller = CreateController();

                controller.ControllerContext.HttpContext = Context.Object;

                return controller;
            });

            Callbacks = new Mock<ICallbackRepository>();
            Connection = new Mock<ConnectionInfo>();
            Context = new Mock<HttpContext>();
            Helper = new ControllerHelper(Callbacks.Object);
            Request = new Mock<HttpRequest>();
            RequestHeaders = new HeaderDictionary();
            Response = new Mock<HttpResponse>();
            ResponseHeaders = new HeaderDictionary();

            Context.SetupGet(c => c.Connection).Returns(Connection.Object);
            Context.SetupGet(c => c.Request).Returns(Request.Object);
            Context.SetupGet(c => c.Response).Returns(Response.Object);
            Request.SetupGet(r => r.Headers).Returns(RequestHeaders);
            Response.SetupGet(r => r.Headers).Returns(ResponseHeaders);
        }

        protected Mock<ICallbackRepository> Callbacks { get; }

        protected Mock<ConnectionInfo> Connection { get; }

        protected Mock<HttpContext> Context { get; }

        protected ControllerHelper Helper { get; }

        protected Mock<HttpRequest> Request { get; }

        protected HeaderDictionary RequestHeaders { get; }

        protected Mock<HttpResponse> Response { get; }

        protected HeaderDictionary ResponseHeaders { get; }

        protected T Subject => this.subject.Value;

        public static void SetHttpContext(ControllerBase controller, Action<HttpContext> modifier = null)
        {
            var httpContext = new DefaultHttpContext();
            if (modifier != null)
            {
                modifier(httpContext);
            }

            if (controller.ControllerContext == null)
            {
                controller.ControllerContext = new ControllerContext();
            }

            controller.ControllerContext.HttpContext = httpContext;
        }

        protected abstract T CreateController();

        protected void MockCallback(Callback callback)
        {
            RequestHeaders.Add(ControllerBaseExtensions.CallbackUrlHeader, callback.Url.ToString());
            Connection.SetupGet(c => c.RemoteIpAddress).Returns(callback.RegisteredIp);

            Callbacks
                .Setup(r => r.AddAsync(callback.RegisteredIp, callback.Url, It.IsAny<CancellationToken>()))
                .ReturnsAsync(callback);
        }
    }
}
