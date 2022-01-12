using ApiShowCase.WebAPI.AddressConstatnts;
using Contracts.Enums;
using Contracts.Logs;
using Contracts.ResponseModels;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.SubdivisionViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.OrganizationEntity
{
    [Area("Admin")]
    [Route("[controller]")]
    public class SubdivisionController : Controller
    {
        private readonly IRequestClient<SubdivisionViewModel> _getAll;
        private readonly IRequestClient<SubdivisionByIdRequest> _getById;
        private readonly IRequestClient<CreateSubdivisionViewModel> _create;
        private readonly IRequestClient<UpdateSubdivisionViewModel> _update;
        private readonly IRequestClient<DeleteSubdivisionRequest> _delete;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMemoryCache _cache;

        public SubdivisionController(IRequestClient<DeleteSubdivisionRequest> delete,
            IRequestClient<SubdivisionViewModel> getAll,
            IRequestClient<SubdivisionByIdRequest> getById,
            IRequestClient<CreateSubdivisionViewModel> create,
            IRequestClient<UpdateSubdivisionViewModel> update,
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
        public async Task<ActionResult<NotificationViewModel<IEnumerable<SubdivisionViewModel>>>> list(bool updateCache)
        {
            if (updateCache) _cache.Remove("listSubdivision");
            IEnumerable<SubdivisionViewModel> subdivisionCache = null;
            if (!_cache.TryGetValue("listSubdivision", out subdivisionCache))
            {
                try
                {
                    var result = await _getAll.GetResponse<AllSubdivisionResponse>(new SubdivisionViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Subdivision + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listSubdivision", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<SubdivisionViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Subdivision + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<SubdivisionViewModel>>(subdivisionCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<SubdivisionViewModel>>> Details(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            
            try
            {
                var result = await _getById.GetResponse<SubdivisionByIdResponse>(new SubdivisionByIdRequest { SubdivisionId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Subdivision + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<SubdivisionViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Subdivision + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateSubdivisionViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            
            try
            {
                var result = await _create.GetResponse<NotificationViewModel>(request);
                if(result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Subdivision + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Subdivision + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdateSubdivisionViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            
            try
            {
                var result = await _update.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Subdivision + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Subdivision + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
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
                var result = await _delete.GetResponse<NotificationViewModel>(new DeleteSubdivisionRequest { SubdivisionId = id });
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Subdivision + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Subdivision + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }
    }
}
