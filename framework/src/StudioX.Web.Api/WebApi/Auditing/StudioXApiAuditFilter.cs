using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using StudioX.Auditing;
using StudioX.Dependency;
using StudioX.WebApi.Validation;

namespace StudioX.WebApi.Auditing
{
    public class StudioXApiAuditFilter : IActionFilter, ITransientDependency
    {
        public bool AllowMultiple => false;

        private readonly IAuditingHelper auditingHelper;

        public StudioXApiAuditFilter(IAuditingHelper auditingHelper)
        {
            this.auditingHelper = auditingHelper;
        }

        public async Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
        {
            var method = actionContext.ActionDescriptor.GetMethodInfoOrNull();
            if (method == null || !ShouldSaveAudit(actionContext))
            {
                return await continuation();
            }

            var auditInfo = auditingHelper.CreateAuditInfo(
                method,
                actionContext.ActionArguments
            );

            var stopwatch = Stopwatch.StartNew();

            try
            {
                return await continuation();
            }
            catch (Exception ex)
            {
                auditInfo.Exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                auditInfo.ExecutionDuration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
                await auditingHelper.SaveAsync(auditInfo);
            }
        }

        private bool ShouldSaveAudit(HttpActionContext context)
        {
            if (context.ActionDescriptor.IsDynamicStudioXAction())
            {
                return false;
            }

            return auditingHelper.ShouldSaveAudit(context.ActionDescriptor.GetMethodInfoOrNull(), true);
        }
    }
}