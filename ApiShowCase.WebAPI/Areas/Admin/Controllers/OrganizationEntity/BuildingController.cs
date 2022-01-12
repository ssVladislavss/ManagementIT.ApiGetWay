using ApiShowCase.WebAPI.AddressConstatnts;
using Contracts.Enums;
using Contracts.Logs;
using Contracts.ResponseModels;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.BuildingViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.OrganizationEntity
{
    [Area("Admin")]
    [Route("[controller]")]
    public class BuildingController : Controller
    {
        private readonly IRequestClient<BuildingViewModel> _getAll;
        private readonly IRequestClient<BuildingByIdRequest> _getById;
        private readonly IRequestClient<CreateBuildingViewModel> _create;
        private readonly IRequestClient<UpdateBuildingViewModel> _update;
        private readonly IRequestClient<DeleteBuildingRequest> _delete;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMemoryCache _cache;

        public BuildingController(IRequestClient<BuildingViewModel> getAll,
            IRequestClient<BuildingByIdRequest> getById,
            IRequestClient<CreateBuildingViewModel> create,
            IRequestClient<UpdateBuildingViewModel> update,
            IRequestClient<DeleteBuildingRequest> delete,
            IPublishEndpoint publishEndpoint,
            IMemoryCache cache)
        {
            _getAll = getAll ?? throw new ArgumentNullException(nameof(getAll));
            _getById = getById ?? throw new ArgumentNullException(nameof(getById));
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _delete = delete ?? throw new ArgumentNullException(nameof(delete));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet]
        [Route("list")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<BuildingViewModel>>>> List(bool updateCache)
        {
            if (updateCache) _cache.Remove("listBuilding");
            IEnumerable<BuildingViewModel> buildingCache = null;
            if(!_cache.TryGetValue("listBuilding", out buildingCache))
            {
                try
                {
                    var result = await _getAll.GetResponse<AllBuildingReponse>(new BuildingViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Building + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listBuilding", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    var response = new NotificationViewModel<IEnumerable<BuildingViewModel>>(result.Message.Model);
                    return Ok(response);
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Building + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<BuildingViewModel>>(buildingCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<BuildingViewModel>>> Details(int id)
        {
            if (id == 0)
            {
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            }
            try
            {
                var result = await _getById.GetResponse<BuildingByIdResponse>(new BuildingByIdRequest { BuildingId = id });
                if(result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Building + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }

                return Ok(new NotificationViewModel<BuildingViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Building + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.InternalServerError}, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateBuildingViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel<CreateBuildingViewModel>(new[] { TypeOfErrors.DataNotValid }, request));
            
            try
            {
                var result = await _create.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Building + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Building + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.InternalServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdateBuildingViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel<UpdateBuildingViewModel>(new[] { TypeOfErrors.DataNotValid}, request));
            

            try
            {
                var result = await _update.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Building + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Building + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.InternalServerError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<NotificationViewModel>> Delete(int id)
        {
            if(id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            
            try
            {
                var result = await _delete.GetResponse<NotificationViewModel>(new DeleteBuildingRequest { BuildingId = id });
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Building + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Building + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.InternalServerError }, type: NotificationType.Warn));
            }
        }
    }
}
