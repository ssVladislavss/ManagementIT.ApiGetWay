using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationContracts.ViewModels.Application.StateModels;
using Contracts.Enums;
using Contracts.ResponseModels;
using Contracts.ViewModels.Application.StateModels;
using MassTransit;
using Contracts.Logs;
using ApiShowCase.WebAPI.AddressConstatnts;
using Microsoft.Extensions.Caching.Memory;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.ManagementItApplication
{
    [Area("Admin")]
    [Route("[controller]")]
    public class StateController : Controller
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IRequestClient<CreateStateViewModel> _clientCreate;
        private readonly IRequestClient<ApplicationStateViewModel> _clientAll;
        private readonly IRequestClient<GetStateByIdRequest> _clientDetails;
        private readonly IRequestClient<UpdateStateViewModel> _clientUpdate;
        private readonly IRequestClient<DeleteStateViewModel> _clientDelete;
        private readonly IMemoryCache _cache;

        public StateController(IPublishEndpoint publishEndpoint,
            IRequestClient<CreateStateViewModel> clientCreate,
            IRequestClient<ApplicationStateViewModel> clientAll,
            IRequestClient<GetStateByIdRequest> clientDetails,
            IRequestClient<UpdateStateViewModel> clientUpdate,
            IRequestClient<DeleteStateViewModel> clientDelete,
            IMemoryCache cache)
        {
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _clientCreate = clientCreate ?? throw new ArgumentNullException(nameof(clientCreate));
            _clientAll = clientAll ?? throw new ArgumentNullException(nameof(clientAll));
            _clientDetails = clientDetails ?? throw new ArgumentNullException(nameof(clientDetails));
            _clientUpdate = clientUpdate ?? throw new ArgumentNullException(nameof(clientUpdate));
            _clientDelete = clientDelete ?? throw new ArgumentNullException(nameof(clientDelete));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }
        [HttpGet]
        [Route("list")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ApplicationStateViewModel>>>> List(bool updateCache)
        {
            if (updateCache) _cache.Remove("listState");
            IEnumerable<ApplicationStateViewModel> stateCache = null;
            if (!_cache.TryGetValue("listState", out stateCache))
            {
                try
                {
                    var result = await _clientAll.GetResponse<AllStateResponse>(new ApplicationStateViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.State + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(new NotificationViewModel(result.Message.Notification.Errors));
                    }
                    _cache.Set("listState", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<ApplicationStateViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.State + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<ApplicationStateViewModel>>(stateCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<ApplicationStateViewModel>>> Details(int id)
        {
            try
            {
                var result = await _clientDetails.GetResponse<GetStateByIdResponse>(new GetStateByIdRequest{Id = id});

                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.State + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(result.Message.Notification.Errors));
                }
                return Ok(new NotificationViewModel<ApplicationStateViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.State + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateStateViewModel request)
        {
            try
            {
                var result = await _clientCreate.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.State + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.State + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdateStateViewModel request)
        {
            try
            {
                var result = await _clientUpdate.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.State + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.State + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<NotificationViewModel>> Delete(int id)
        {
            try
            {
                var result = await _clientDelete.GetResponse<NotificationViewModel>(new DeleteStateViewModel{Id = id});
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.State + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.State + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }
    }
}
