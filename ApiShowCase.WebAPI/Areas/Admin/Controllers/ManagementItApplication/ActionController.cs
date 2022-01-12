using ApiShowCase.WebAPI.AddressConstatnts;
using ApplicationContracts.ViewModels.Application.ActionModels;
using Contracts.Enums;
using Contracts.Logs;
using Contracts.ResponseModels;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.ManagementItApplication
{
    [Area("Admin")]
    [Route("[controller]")]
    public class ActionController : Controller
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IRequestClient<ActionViewModel> _clientAll;
        private readonly IRequestClient<ActionByIdRequest> _clientDetails;
        private readonly IRequestClient<DeleteRangeActionRequest> _deleteRange;
        private readonly IRequestClient<ActionByEnumTypeRequest> _clientByEnum;
        private readonly IRequestClient<DeleteSelectActionRequest> _deleteSelect;
        private readonly IRequestClient<ActionByApplicationIdRequest> _listByAppId;
        private readonly IMemoryCache _cache;
        public ActionController(IRequestClient<ActionByIdRequest> clientDetails,
            IRequestClient<ActionViewModel> clientAll,
            IPublishEndpoint publishEndpoint,
            IRequestClient<ActionByEnumTypeRequest> clientByEnum,
            IRequestClient<DeleteRangeActionRequest> deleteRange,
            IRequestClient<ActionByApplicationIdRequest> listByAppId,
            IRequestClient<DeleteSelectActionRequest> deleteSelect,
            IMemoryCache cache)
        {
            _clientDetails = clientDetails ?? throw new ArgumentNullException(nameof(clientDetails));
            _clientAll = clientAll ?? throw new ArgumentNullException(nameof(clientAll));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _clientByEnum = clientByEnum ?? throw new ArgumentNullException(nameof(clientByEnum));
            _deleteRange = deleteRange ?? throw new ArgumentNullException(nameof(deleteRange));
            _listByAppId = listByAppId ?? throw new ArgumentNullException(nameof(listByAppId));
            _deleteSelect = deleteSelect ?? throw new ArgumentNullException(nameof(deleteSelect));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet]
        [Route("list")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ActionViewModel>>>> List(bool updateCache)
        {
            if (updateCache) _cache.Remove("listAction");
            IEnumerable<ActionViewModel> actionCache = null;
            if (!_cache.TryGetValue("listAction", out actionCache))
            {
                try
                {
                    var result = await _clientAll.GetResponse<AllActionResponse>(new ActionViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Action + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(new NotificationViewModel(result.Message.Notification.Errors));
                    }
                    _cache.Set("listAction", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<ActionViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Action + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<ActionViewModel>>(actionCache));
        }

        [HttpGet]
        [Route("listOrAppId")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ActionViewModel>>>> ListActionOrApplication(int id)
        {
            if (id == 0) return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            try
            {
                var result = await _listByAppId.GetResponse<AllActionResponse>(new ActionByApplicationIdRequest { ApplicationId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Action + "/listOrAppId", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(result.Message.Notification.Errors));
                }
                return Ok(new NotificationViewModel<IEnumerable<ActionViewModel>>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Action + "/listOrAppId", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<ActionViewModel>>> Details(ActionByIdRequest request)
        {
            if (request.ActionId == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var result = await _clientDetails.GetResponse<ActionByIdResponse>(request);
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Action + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(result.Message.Notification.Errors));
                }

                return Ok(new NotificationViewModel<ActionViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Action + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("listByActionType")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ActionViewModel>>>> ListByActionType(ActionByEnumTypeRequest request)
        {
            if (request.NumberType == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var result = await _clientByEnum.GetResponse<AllActionResponse>(request);
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Action + "/listByActionType", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(result.Message.Notification.Errors));
                }

                return Ok(new NotificationViewModel<IEnumerable<ActionViewModel>>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Action + "/listByActionType", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("deleteRange")]
        public async Task<ActionResult<NotificationViewModel>> DeleteRange()
        {
            try
            {
                var result = await _deleteRange.GetResponse<NotificationViewModel>(new DeleteRangeActionRequest());
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Action + "/deleteRange", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }

                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Action + "/deleteRange", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("deleteSelected")]
        public async Task<ActionResult<NotificationViewModel>> DeleteSelected(string jsonIds)
        {
            var request = JsonSerializer.Deserialize<DeleteSelectActionRequest>(jsonIds);
            if (request.IdsAction == null) return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            if(!request.IdsAction.Any()) return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            try
            {
                var result = await _deleteSelect.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Action + "/deleteSelected", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }

                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Action + "/deleteSelected", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }
    }
}

