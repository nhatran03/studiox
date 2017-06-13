﻿using System.Linq;
using System.Reflection;
using StudioX.Application.Services;
using StudioX.AspNetCore.Configuration;
using StudioX.AspNetCore.Mvc.Extensions;
using StudioX.AspNetCore.Mvc.Proxying.Utils;
using StudioX.Dependency;
using StudioX.Extensions;
using StudioX.Reflection.Extensions;
using StudioX.Web.Api.Modeling;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace StudioX.AspNetCore.Mvc.Proxying
{
    public class AspNetCoreApiDescriptionModelProvider : IApiDescriptionModelProvider, ISingletonDependency
    {
        public ILogger Logger { get; set; }

        private readonly IApiDescriptionGroupCollectionProvider descriptionProvider;
        private readonly StudioXAspNetCoreConfiguration configuration;

        public AspNetCoreApiDescriptionModelProvider(
            IApiDescriptionGroupCollectionProvider descriptionProvider,
            StudioXAspNetCoreConfiguration configuration)
        {
            this.descriptionProvider = descriptionProvider;
            this.configuration = configuration;

            Logger = NullLogger.Instance;
        }

        public ApplicationApiDescriptionModel CreateModel()
        {
            var model = new ApplicationApiDescriptionModel();

            foreach (var descriptionGroupItem in descriptionProvider.ApiDescriptionGroups.Items)
            {
                foreach (var apiDescription in descriptionGroupItem.Items)
                {
                    AddApiDescriptionToModel(apiDescription, model);
                }
            }

            return model;
        }

        private void AddApiDescriptionToModel(ApiDescription apiDescription, ApplicationApiDescriptionModel model)
        {
            var moduleModel = model.GetOrAddModule(GetModuleName(apiDescription));
            var controllerModel = moduleModel.GetOrAddController(GetControllerName(apiDescription));

            var method = apiDescription.ActionDescriptor.GetMethodInfo();

            if (controllerModel.Actions.ContainsKey(method.Name))
            {
                Logger.Warn($"Controller '{controllerModel.Name}' contains more than one action with name '{method.Name}' for module '{moduleModel.Name}'. Ignored: " + apiDescription.ActionDescriptor.GetMethodInfo());
                return;
            }

            var returnValue = new ReturnValueApiDescriptionModel(method.ReturnType);

            var actionModel = controllerModel.AddAction(new ActionApiDescriptionModel(
                method.Name,
                returnValue,
                apiDescription.RelativePath,
                apiDescription.HttpMethod
            ));

            AddParameterDescriptionsToModel(actionModel, method, apiDescription);
        }

        private static string GetControllerName(ApiDescription apiDescription)
        {
            return apiDescription.GroupName?.RemovePostFix(ApplicationService.CommonPostfixes) 
                   ?? apiDescription.ActionDescriptor.AsControllerActionDescriptor().ControllerName;
        }

        private void AddParameterDescriptionsToModel(ActionApiDescriptionModel actionModel, MethodInfo method, ApiDescription apiDescription)
        {
            if (!apiDescription.ParameterDescriptions.Any())
            {
                return;
            }

            var matchedMethodParamNames = ArrayMatcher.Match(
                apiDescription.ParameterDescriptions.Select(p => p.Name).ToArray(),
                method.GetParameters().Select(GetMethodParamName).ToArray()
            );

            for (var i = 0; i < apiDescription.ParameterDescriptions.Count; i++)
            {
                var parameterDescription = apiDescription.ParameterDescriptions[i];
                var matchedMethodParamName = matchedMethodParamNames.Length > i
                                                 ? matchedMethodParamNames[i]
                                                 : parameterDescription.Name;

                actionModel.AddParameter(new ParameterApiDescriptionModel(
                        parameterDescription.Name,
                        matchedMethodParamName,
                        parameterDescription.Type,
                        parameterDescription.RouteInfo?.IsOptional ?? false,
                        parameterDescription.RouteInfo?.DefaultValue,
                        parameterDescription.RouteInfo?.Constraints?.Select(c => c.GetType().Name).ToArray(),
                        parameterDescription.Source.Id
                    )
                );
            }
        }

        public string GetMethodParamName(ParameterInfo parameterInfo)
        {
            var modelNameProvider = parameterInfo.GetCustomAttributes()
                .OfType<IModelNameProvider>()
                .FirstOrDefault();

            if (modelNameProvider == null)
            {
                return parameterInfo.Name;
            }

            return modelNameProvider.Name;
        }

        private string GetModuleName(ApiDescription apiDescription)
        {
            var controllerType = apiDescription.ActionDescriptor.AsControllerActionDescriptor().ControllerTypeInfo.AsType();
            if (controllerType == null)
            {
                return StudioXControllerAssemblySetting.DefaultServiceModuleName;
            }

            foreach (var controllerSetting in configuration.ControllerAssemblySettings)
            {
                if (Equals(controllerType.GetAssembly(), controllerSetting.Assembly))
                {
                    return controllerSetting.ModuleName;
                }
            }

            return StudioXControllerAssemblySetting.DefaultServiceModuleName;
        }
    }
}