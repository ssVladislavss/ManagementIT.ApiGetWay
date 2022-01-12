using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ApiShowCase.WebAPI.Models.Application;
using ApplicationContracts.ViewModels.Application.ApplicationToItModels;
using Contracts.Enums;
using Contracts.ResponseModels;
using Microsoft.AspNetCore.Authorization;
using MassTransit;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.Application;
using Contracts.Logs;
using ApiShowCase.WebAPI.AddressConstatnts;
using System.Collections.Generic;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.EmployeeViewModels;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.ManagementItApplication
{
    [Area("Admin")]
    [Route("[controller]")]
    public class ApplicationToItController : Controller
    {
        private readonly IRequestClient<ApplicationToItViewModel> _clientAll;
        private readonly IRequestClient<GetApplicationByDeptIdRequest> _clientAllByDept;
        private readonly IRequestClient<GetApplicationForOnDeleteViewModel> _clientAllForOnDelete;
        private readonly IRequestClient<ApplicationByIdRequest> _clientById;
        private readonly IRequestClient<EditToItStateViewModel> _clientUpdateState;
        private readonly IRequestClient<EditPriorityOrApplicationViewModel> _clientUpdatePriority;
        private readonly IRequestClient<OnDeleteApplicationViewModel> _clientActiveOnDelete;
        private readonly IRequestClient<DeleteApplicationViewModel> _clientDelete;
        private readonly IRequestClient<DeleteRange> _clientDeleteRange;
        private readonly IRequestClient<GetCreateForApplicationRequest> _clientCreateForAppByOrgEntity;
        private readonly IRequestClient<GetCreateApplicationViewModel> _clientGetCreate;
        private readonly IRequestClient<CreateApplicationToITViewModel> _clientCreate;
        private readonly IRequestClient<GetUpdateApplicationRequest> _clientGetUpdate;
        private readonly IRequestClient<UpdateApplicationViewModel> _clientUpdate;
        private readonly IRequestClient<UpdateEmployeeOrApplicationRequest> _updateEmployee;
        private readonly IRequestClient<GetEmployeeByUserNameRequest> _getByUserName;
        private readonly IRequestClient<SetIniciatorOrApplicationRequest> _setIniciator;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMemoryCache _cache;

        public ApplicationToItController(IRequestClient<ApplicationToItViewModel> clientAll,
            IRequestClient<GetApplicationByDeptIdRequest> clientAllByDept,
            IRequestClient<GetApplicationForOnDeleteViewModel> clientAllForonDelete,
            IRequestClient<EditToItStateViewModel> clientUpdateState,
            IRequestClient<OnDeleteApplicationViewModel> clientActiveOnDelete,
            IRequestClient<DeleteApplicationViewModel> clientDelete,
            IRequestClient<DeleteRange> clientDeleteRange,
            IRequestClient<ApplicationByIdRequest> clientById,
            IRequestClient<GetCreateForApplicationRequest> clientCreateForAppByOrgEntity,
            IRequestClient<GetCreateApplicationViewModel> clientGetCreate,
            IRequestClient<CreateApplicationToITViewModel> clientCreate,
            IRequestClient<GetUpdateApplicationRequest> clientGetUpdate,
            IRequestClient<UpdateApplicationViewModel> clientUpdate,
            IPublishEndpoint publishEndpoint,
            IRequestClient<EditPriorityOrApplicationViewModel> clientUpdatePriority,
            IRequestClient<UpdateEmployeeOrApplicationRequest> updateEmployee,
            IRequestClient<GetEmployeeByUserNameRequest> getByUserName,
            IRequestClient<SetIniciatorOrApplicationRequest> setIniciator,
            IMemoryCache cache)
        {
            _clientAll = clientAll ?? throw new ArgumentNullException(nameof(clientAll));
            _clientAllByDept = clientAllByDept ?? throw new ArgumentNullException(nameof(clientAllByDept));
            _clientAllForOnDelete = clientAllForonDelete ?? throw new ArgumentNullException(nameof(clientAllForonDelete));
            _clientUpdateState = clientUpdateState ?? throw new ArgumentNullException(nameof(clientUpdateState));
            _clientActiveOnDelete = clientActiveOnDelete ?? throw new ArgumentNullException(nameof(clientActiveOnDelete));
            _clientDelete = clientDelete ?? throw new ArgumentNullException(nameof(clientDelete));
            _clientDeleteRange = clientDeleteRange ?? throw new ArgumentNullException(nameof(clientDeleteRange));
            _clientById = clientById ?? throw new ArgumentNullException(nameof(clientById));
            _clientCreateForAppByOrgEntity = clientCreateForAppByOrgEntity ?? throw new ArgumentNullException(nameof(clientCreateForAppByOrgEntity));
            _clientGetCreate = clientGetCreate ?? throw new ArgumentNullException(nameof(clientGetCreate));
            _clientCreate = clientCreate ?? throw new ArgumentNullException(nameof(clientCreate));
            _clientGetUpdate = clientGetUpdate ?? throw new ArgumentNullException(nameof(clientGetUpdate));
            _clientUpdate = clientUpdate ?? throw new ArgumentNullException(nameof(clientUpdate));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _clientUpdatePriority = clientUpdatePriority ?? throw new ArgumentNullException(nameof(clientUpdatePriority));
            _updateEmployee = updateEmployee ?? throw new ArgumentNullException(nameof(updateEmployee));
            _getByUserName = getByUserName ?? throw new ArgumentNullException(nameof(getByUserName));
            _setIniciator = setIniciator ?? throw new ArgumentNullException(nameof(setIniciator));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }


        [HttpGet("list/{deptId}")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ApplicationToItViewModel>>>> List(int deptId)
        {
            if (deptId == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var result = await _clientAllByDept.GetResponse<AllApplicationResponse>(new GetApplicationByDeptIdRequest { DeptId = deptId });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/listbyDeptId", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<IEnumerable<ApplicationToItViewModel>>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/listbyDeptId", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("list")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ApplicationToItViewModel>>>> List(bool updateCache)
        {
            if (updateCache) _cache.Remove("listApplication");
            IEnumerable<ApplicationToItViewModel> applicationCache = null;
            if (!_cache.TryGetValue("listApplication", out applicationCache))
            {
                try
                {
                    var result = await _clientAll.GetResponse<AllApplicationResponse>(new ApplicationToItViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.ApplicationToIt + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listApplication", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<ApplicationToItViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<ApplicationToItViewModel>>(applicationCache));
        }

        [HttpGet]
        [Route("listForOnDelete")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<ApplicationToItViewModel>>>> ListForOnDelete(bool updateCache)
        {
            if (updateCache) _cache.Remove("listApplicationOnDelete");
            IEnumerable<ApplicationToItViewModel> applicationCache = null;
            if (!_cache.TryGetValue("listApplicationOnDelete", out applicationCache))
            {
                try
                {
                    var result = await _clientAllForOnDelete.GetResponse<AllApplicationResponse>(new GetApplicationForOnDeleteViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.ApplicationToIt + "/listForOnDelete", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listApplicationOnDelete", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<ApplicationToItViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/listForOnDelete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<ApplicationToItViewModel>>(applicationCache));
        }

        [HttpGet]
        [Route("details")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel<ApplicationToItViewModel>>> Details(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var result = await _clientById.GetResponse<ApplicationByIdResponse>(new ApplicationByIdRequest { Id = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<ApplicationToItViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpGet("create")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel<GetCreateOrEditApplicationViewModel>>> Create()
        {
            try
            {
                var appResult =
                    await _clientGetCreate.GetResponse<GetCreateApplicationResponse>(
                        new GetCreateApplicationViewModel());
                if (appResult.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/getCreate", appResult.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(appResult.Message.Notification);
                }

                var orgResult =
                    await _clientCreateForAppByOrgEntity.GetResponse<GetCreateForApplicationResponse>(
                        new GetCreateForApplicationRequest());
                if (orgResult.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/getCreate", orgResult.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(orgResult.Message.Notification);
                }

                var response = new GetCreateOrEditApplicationViewModel(appResult.Message.Model, orgResult.Message);
                return Ok(new NotificationViewModel<GetCreateOrEditApplicationViewModel>(response));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/getCreate", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.InternalServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateApplicationToITViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                request.IniciatorUserName = HttpContext.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(request.IniciatorUserName))
                {
                    var iniciatorResult = await _getByUserName.GetResponse<EmployeeByIdResponse>
                     (new GetEmployeeByUserNameRequest { UserName = request.IniciatorUserName });
                    if (iniciatorResult.Message.Notification.Type == NotificationType.Success)
                    {
                        request.IniciatorFullName = $"{iniciatorResult.Message.Model.Surname} {iniciatorResult.Message.Model.Name} {iniciatorResult.Message.Model.Patronymic}";
                        request.Contact = iniciatorResult.Message.Model.WorkTelephone;
                        request.IniciatorId = iniciatorResult.Message.Model.Id;
                    }
                }
                else
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/create",
                        $"Создание заявки || Невозможно определить инициатора создания || Убедитесь, что у Вас корректно работает система авторизации и, что пользователь авторизован || \nОписание заявки: < {request.Content} > || \nID типа заявки: < {request.ApplicationTypeId } > || \nНазвание отделения: < {request.DepartamentName } > || \nНазвание помещения: < {request.RoomName} >", NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }

                var result = await _clientCreate.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.InternalServerError }, type: NotificationType.Warn));
            }
        }

        [HttpGet("update")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel<GetCreateOrEditApplicationViewModel>>> Update(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var appResult =
                    await _clientGetUpdate.GetResponse<GetUpdateApplicationResponse>(
                        new GetUpdateApplicationRequest { ApplicationId = id });
                if (appResult.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/getUpdate", appResult.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(appResult.Message.Notification);
                }

                var orgResult =
                    await _clientCreateForAppByOrgEntity.GetResponse<GetCreateForApplicationResponse>(
                        new GetCreateForApplicationRequest());
                if (orgResult.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/getUpdate", orgResult.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(orgResult.Message.Notification);
                }

                var response = new GetCreateOrEditApplicationViewModel(appResult.Message.Model, orgResult.Message);
                return Ok(new NotificationViewModel<GetCreateOrEditApplicationViewModel>(response));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/getUpdate", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.InternalServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdateApplicationViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                request.IniciatorUserName = HttpContext.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(request.IniciatorUserName))
                {
                    var iniciatorResult = await _getByUserName.GetResponse<EmployeeByIdResponse>
                     (new GetEmployeeByUserNameRequest { UserName = request.IniciatorUserName });
                    if (iniciatorResult.Message.Notification.Type == NotificationType.Success)
                    {
                        request.IniciatorFullName = $"{iniciatorResult.Message.Model.Surname} {iniciatorResult.Message.Model.Name} {iniciatorResult.Message.Model.Patronymic}";
                        request.IniciatorId = iniciatorResult.Message.Model.Id;
                    }
                }
                var result = await _clientUpdate.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("updateState")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> UpdateState(EditToItStateViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            try
            {
                request.IniciatorUserName = HttpContext.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(request.IniciatorUserName))
                {
                    var iniciatorResult = await _getByUserName.GetResponse<EmployeeByIdResponse>
                     (new GetEmployeeByUserNameRequest { UserName = request.IniciatorUserName });
                    if (iniciatorResult.Message.Notification.Type == NotificationType.Success)
                    {
                        request.IniciatorFullName = $"{iniciatorResult.Message.Model.Surname} {iniciatorResult.Message.Model.Name} {iniciatorResult.Message.Model.Patronymic}";
                        request.IniciatorId = iniciatorResult.Message.Model.Id;
                    }
                }
                var result = await _clientUpdateState.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/updateState", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/updateState", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("updateEmployee")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> UpdateEmployee(UpdateEmployeeOrApplicationRequest request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            try
            {
                request.Iniciator = HttpContext.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(request.Iniciator))
                {
                    var iniciatorResult = await _getByUserName.GetResponse<EmployeeByIdResponse>
                     (new GetEmployeeByUserNameRequest { UserName = request.Iniciator });
                    if (iniciatorResult.Message.Notification.Type == NotificationType.Success)
                    {
                        request.IniciatorFullName = $"{iniciatorResult.Message.Model.Surname} {iniciatorResult.Message.Model.Name} {iniciatorResult.Message.Model.Patronymic}";
                        request.IniciatorId = iniciatorResult.Message.Model.Id;
                    }
                }
                var result = await _updateEmployee.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/updateEmployee", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/updateEmployee", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("updatePriority")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> UpdatePriority(EditPriorityOrApplicationViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            try
            {
                request.IniciatorUserName = HttpContext.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(request.IniciatorUserName))
                {
                    var iniciatorResult = await _getByUserName.GetResponse<EmployeeByIdResponse>
                     (new GetEmployeeByUserNameRequest { UserName = request.IniciatorUserName });
                    if (iniciatorResult.Message.Notification.Type == NotificationType.Success)
                    {
                        request.IniciatorFullName = $"{iniciatorResult.Message.Model.Surname} {iniciatorResult.Message.Model.Name} {iniciatorResult.Message.Model.Patronymic}";
                        request.IniciatorId = iniciatorResult.Message.Model.Id;
                    }
                }
                var result = await _clientUpdatePriority.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/updatePriority", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/updatePriority", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("onDelete")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> OnDelete(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            try
            {
                var userNameIniciator = HttpContext.User?.Identity?.Name;
                var request = new OnDeleteApplicationViewModel { Id = id, IniciatorUserName = userNameIniciator };
                if (!string.IsNullOrEmpty(userNameIniciator))
                {
                    var iniciatorResult = await _getByUserName.GetResponse<EmployeeByIdResponse>
                     (new GetEmployeeByUserNameRequest { UserName = userNameIniciator });
                    if (iniciatorResult.Message.Notification.Type == NotificationType.Success)
                    {
                        request.IniciatorFullName = $"{iniciatorResult.Message.Model.Surname} {iniciatorResult.Message.Model.Name} {iniciatorResult.Message.Model.Patronymic}";
                        request.IniciatorId = iniciatorResult.Message.Model.Id;
                    }
                }
                var result = await _clientActiveOnDelete.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/onDelete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/onDelete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("delete")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> Delete(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var userNameIniciator = HttpContext.User?.Identity?.Name;
                var request = new DeleteApplicationViewModel { Id = id, IniciatorUserName = userNameIniciator };
                if (!string.IsNullOrEmpty(userNameIniciator))
                {
                    var iniciatorResult = await _getByUserName.GetResponse<EmployeeByIdResponse>
                     (new GetEmployeeByUserNameRequest { UserName = userNameIniciator });
                    if (iniciatorResult.Message.Notification.Type == NotificationType.Success)
                    {
                        request.IniciatorFullName = $"{iniciatorResult.Message.Model.Surname} {iniciatorResult.Message.Model.Name} {iniciatorResult.Message.Model.Patronymic}";
                        request.IniciatorId = iniciatorResult.Message.Model.Id;
                    }
                }
                var result = await _clientDelete.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("deleteRange")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> DeleteRange()
        {
            try
            {
                var result = await _clientDeleteRange.GetResponse<NotificationViewModel>(new DeleteRange());
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/deleteRange", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/deleteRange", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("setIniciator")]
        //[Authorize(Roles = "Admin, RootAdmin")]
        public async Task<ActionResult<NotificationViewModel>> SetIniciator(SetIniciatorOrApplicationRequest request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));
            try
            {
                var result = await _setIniciator.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.ApplicationToIt + "/setIniciator", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.ApplicationToIt + "/setIniciator", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ApplicationServerError }, type: NotificationType.Warn));
            }
        }
    }
}
