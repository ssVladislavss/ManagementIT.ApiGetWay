using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ApplicationContracts.ViewModels.Application.Priority;
using Contracts.Enums;
using Contracts.ResponseModels;
using MassTransit;
using System.Collections.Generic;
using Contracts.Logs;
using ApiShowCase.WebAPI.AddressConstatnts;
using Microsoft.Extensions.Caching.Memory;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.ManagementItApplication
{
    [Area("Admin")]
    [Route("[controller]")]
    public class PriorityController : Controller
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IRequestClient<CreateOrEditApplicationPriorityViewModel> _clientCreate;
        private readonly IRequestClient<ApplicationPriorityViewModel> _clientAll;
        private readonly IRequestClient<PriorityByIdRequest> _clientDetails;
        private readonly IRequestClient<UpdatePriorityViewModel> _clientUpdate;
        private readonly IRequestClient<DeletePriorityViewModel> _clientDelete;
        private readonly IMemoryCache _cache;

        public PriorityController(IPublishEndpoint publishEndpoint,
            IRequestClient<CreateOrEditApplicationPriorityViewModel> clientCreate,
            IRequestClient<ApplicationPriorityViewModel> clientAll,
            IRequestClient<PriorityByIdRequest> clientDetails,
            IRequestClient<UpdatePriorityViewModel> clientUpdate,
            IRequestClient<DeletePriorityViewModel> clientDelete,
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
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ApplicationPriorityViewModel>>>> List(bool updateCache)
        {
            if (updateCache) _cache.Remove("listPriority");
            IEnumerable<ApplicationPriorityViewModel> priorityCache = null;
            if (!_cache.TryGetValue("listPriority", out priorityCache))
            {
                try
                {
                    var result = await _clientAll.GetResponse<AllPriorityResponse>(new ApplicationPriorityViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Priority + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listPriority", result.Message.priorities, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<ApplicationPriorityViewModel>>(result.Message.priorities));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Priority + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<ApplicationPriorityViewModel>>(priorityCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<ApplicationPriorityViewModel>>> Details(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            try
            {
                var result = await _clientDetails.GetResponse<PriorityByIdResponse>(new PriorityByIdRequest { Id = id });
                if(result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Priority + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                
                return Ok(new NotificationViewModel<ApplicationPriorityViewModel>(result.Message.model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Priority + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] {TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateOrEditApplicationPriorityViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel<CreateOrEditApplicationPriorityViewModel>(new[] { TypeOfErrors.DataNotValid }, request));
            try
            {
                var result = await _clientCreate.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Priority + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }

                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Priority + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
            
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdatePriorityViewModel request)
        {
            if (!ModelState.IsValid || request.Id == 0)
                return Ok(new NotificationViewModel<UpdatePriorityViewModel>(new[] { TypeOfErrors.DataNotValid }, request));

            try
            {
                var result = await _clientUpdate.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Priority + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Priority + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
            
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<NotificationViewModel>> Delete(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var result = await _clientDelete.GetResponse<NotificationViewModel>(new DeletePriorityViewModel { Id = id });
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Priority + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Priority + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }
    }
}
