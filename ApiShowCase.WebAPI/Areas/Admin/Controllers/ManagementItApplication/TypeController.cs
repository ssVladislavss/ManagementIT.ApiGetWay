using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationContracts.ViewModels.Application.TypeModels;
using Contracts.Enums;
using Contracts.ResponseModels;
using MassTransit;
using Contracts.Logs;
using ApiShowCase.WebAPI.AddressConstatnts;
using Microsoft.Extensions.Caching.Memory;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.ManagementItApplication
{
    [Area("Admin")]
    [Route("[controller]")]
    public class TypeController : Controller
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IRequestClient<CreateTypeViewModel> _clientCreate;
        private readonly IRequestClient<ApplicationTypeViewModel> _clientAll;
        private readonly IRequestClient<TypeByIdRequest> _clientDetails;
        private readonly IRequestClient<UpdateTypeViewModel> _clientUpdate;
        private readonly IRequestClient<DeleteTypeViewModel> _clientDelete;
        private readonly IMemoryCache _cache;

        public TypeController(IPublishEndpoint publishEndpoint,
            IRequestClient<CreateTypeViewModel> clientCreate,
            IRequestClient<ApplicationTypeViewModel> clientAll,
            IRequestClient<TypeByIdRequest> clientDetails,
            IRequestClient<UpdateTypeViewModel> clientUpdate,
            IRequestClient<DeleteTypeViewModel> clientDelete,
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
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ApplicationTypeViewModel>>>> List(bool updateCache)
        {
            if (updateCache) _cache.Remove("listType");
            IEnumerable<ApplicationTypeViewModel> stateCache = null;
            if (!_cache.TryGetValue("listType", out stateCache))
            {
                try
                {
                    var result = await _clientAll.GetResponse<AllTypeResponse>(new ApplicationTypeViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Type + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(new NotificationViewModel(result.Message.Notification.Errors));
                    }
                    _cache.Set("listType", result.Message.model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<ApplicationTypeViewModel>>(result.Message.model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Type + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<ApplicationTypeViewModel>>(stateCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<ApplicationTypeViewModel>>> Details(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            try
            {
                var result = await _clientDetails.GetResponse<TypeByIdResponse>(new TypeByIdRequest() { Id = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Type + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(result.Message.Notification.Errors));
                }
                return Ok(new NotificationViewModel<ApplicationTypeViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Type + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateTypeViewModel request)
        {
            try
            {
                var result = await _clientCreate.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Type + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Type + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdateTypeViewModel request)
        {
            try
            {
                var result = await _clientUpdate.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Type + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Type + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] {TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<NotificationViewModel>> Delete(int id)
        {
            try
            {
                var result = await _clientDelete.GetResponse<NotificationViewModel>(new DeleteTypeViewModel{Id = id});
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Type + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Type + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }
    }
}
