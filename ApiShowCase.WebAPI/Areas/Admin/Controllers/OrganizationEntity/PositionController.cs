using ApiShowCase.WebAPI.AddressConstatnts;
using Contracts.Enums;
using Contracts.Logs;
using Contracts.ResponseModels;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.PositionViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.OrganizationEntity
{
    [Area("Admin")]
    [Route("[controller]")]
    public class PositionController : Controller
    {
        private readonly IRequestClient<PositionViewModel> _getAll;
        private readonly IRequestClient<PositionByIdRequest> _getById;
        private readonly IRequestClient<CreatePositionViewModel> _create;
        private readonly IRequestClient<UpdatePositionViewModel> _update;
        private readonly IRequestClient<DeletePositionRequest> _delete;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMemoryCache _cache;

        public PositionController(IRequestClient<DeletePositionRequest> delete,
            IRequestClient<PositionViewModel> getAll,
            IRequestClient<PositionByIdRequest> getById,
            IRequestClient<CreatePositionViewModel> create,
            IRequestClient<UpdatePositionViewModel> update,
            IPublishEndpoint publishEndpoint,
            IMemoryCache cache)
        {
            _delete = delete ?? throw new ArgumentNullException(nameof(delete));
            _getAll = getAll ?? throw new ArgumentNullException(nameof(getAll));
            _getById = getById ?? throw new ArgumentNullException(nameof(getById));
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet]
        [Route("list")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<PositionViewModel>>>> list(bool updateCache)
        {
            if (updateCache) _cache.Remove("listPosition");
            IEnumerable<PositionViewModel> positionCache = null;
            if (!_cache.TryGetValue("listPosition", out positionCache))
            {
                try
                {
                    var result = await _getAll.GetResponse<AllPositionResponse>(new PositionViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Position + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listPosition", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<PositionViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Position + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<PositionViewModel>>(positionCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<PositionViewModel>>> Details(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            
            try
            {
                var result = await _getById.GetResponse<PositionByIdResponse>(new PositionByIdRequest { PositionId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Position + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<PositionViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Position + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreatePositionViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            
            try
            {
                var result = await _create.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Position + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Position + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdatePositionViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            
            try
            {
                var result = await _update.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Position + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Position + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
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
                var result = await _delete.GetResponse<NotificationViewModel>(new DeletePositionRequest { PositionId = id });
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Position + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Position + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }
    }
}
