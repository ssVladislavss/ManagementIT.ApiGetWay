using ApiShowCase.WebAPI.AddressConstatnts;
using ApplicationContracts.ViewModels.Application.ApplicationToItModels;
using ApplicationContracts.ViewModels.Application.ExistDependency;
using Contracts.Enums;
using Contracts.Logs;
using Contracts.ResponseModels;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.EmployeeViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiShowCase.WebAPI.Areas.Admin.Controllers.OrganizationEntity
{
    [Area("Admin")]
    [Route("[controller]")]
    public class EmployeeController : Controller
    {
        private readonly IRequestClient<EmployeeViewModel> _getAll;
        private readonly IRequestClient<EmployeeByIdRequest> _getById;
        private readonly IRequestClient<CreateEmployeeViewModel> _create;
        private readonly IRequestClient<UpdateEmployeeViewModel> _update;
        private readonly IRequestClient<DeleteEmployeeRequest> _delete;
        private readonly IRequestClient<GetCreateEmployeeRequest> _getCreate;
        private readonly IRequestClient<GetUpdateEmployeeRequest> _getUpdate;
        private readonly IRequestClient<DeleteEmployeePhotoRequest> _deletePhoto;
        private readonly IRequestClient<CreateOrEditEmployeePhotoViewModel> _updatePhoto;
        private readonly IRequestClient<GetEmployeeByUserNameRequest> _getByUserName;
        private readonly IRequestClient<ExistDependenceEntityRequest> _existDependency;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IMemoryCache _cache;

        public EmployeeController(IRequestClient<CreateOrEditEmployeePhotoViewModel> updatePhoto,
            IRequestClient<EmployeeViewModel> getAll,
            IRequestClient<EmployeeByIdRequest> getById,
            IRequestClient<CreateEmployeeViewModel> create,
            IRequestClient<UpdateEmployeeViewModel> update,
            IRequestClient<DeleteEmployeeRequest> delete,
            IRequestClient<GetCreateEmployeeRequest> getCreate,
            IRequestClient<GetUpdateEmployeeRequest> getUpdate,
            IRequestClient<DeleteEmployeePhotoRequest> deletePhoto,
            IPublishEndpoint publishEndpoint,
            IRequestClient<ExistDependenceEntityRequest> existDependency,
            IMemoryCache cache)
        {
            _updatePhoto = updatePhoto ?? throw new ArgumentNullException(nameof(updatePhoto));
            _getAll = getAll ?? throw new ArgumentNullException(nameof(getAll));
            _getById = getById ?? throw new ArgumentNullException(nameof(getById));
            _create = create ?? throw new ArgumentNullException(nameof(create));
            _update = update ?? throw new ArgumentNullException(nameof(update));
            _delete = delete ?? throw new ArgumentNullException(nameof(delete));
            _getCreate = getCreate ?? throw new ArgumentNullException(nameof(getCreate));
            _getUpdate = getUpdate ?? throw new ArgumentNullException(nameof(getUpdate));
            _deletePhoto = deletePhoto ?? throw new ArgumentNullException(nameof(deletePhoto));
            _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
            _existDependency = existDependency ?? throw new ArgumentNullException(nameof(existDependency));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet]
        [Route("list")]
        public async Task<ActionResult<NotificationViewModel<IEnumerable<EmployeeViewModel>>>> list(bool updateCache)
        {
            if (updateCache) _cache.Remove("listEmployee");
            IEnumerable<EmployeeViewModel> employeeCache = null;
            if (!_cache.TryGetValue("listEmployee", out employeeCache))
            {
                try
                {
                    var result = await _getAll.GetResponse<AllEmployeeResponse>(new EmployeeViewModel());
                    if (result.Message.Notification.Type != NotificationType.Success)
                    {
                        var message = new CreateLog(AddressConst.Employee + "/list", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                        await _publishEndpoint.Publish(message);
                        return Ok(result.Message.Notification);
                    }
                    _cache.Set("listEmployee", result.Message.Model, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                    });
                    return Ok(new NotificationViewModel<IEnumerable<EmployeeViewModel>>(result.Message.Model));
                }
                catch (Exception e)
                {
                    var message = new CreateLog(AddressConst.Employee + "/list", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
                }
            }
            return Ok(new NotificationViewModel<IEnumerable<EmployeeViewModel>>(employeeCache));
        }

        [HttpGet]
        [Route("details")]
        public async Task<ActionResult<NotificationViewModel<EmployeeViewModel>>> Details(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var result = await _getById.GetResponse<EmployeeByIdResponse>(new EmployeeByIdRequest { EmployeeId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/details", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<EmployeeViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/details", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("byUserName")]
        public async Task<ActionResult<NotificationViewModel<EmployeeViewModel>>> EmployeeByUserName(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var result = await _getByUserName.GetResponse<EmployeeByIdResponse>(new GetEmployeeByUserNameRequest { UserName = userName });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/byUserName", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<EmployeeViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/byUserName", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel<CreateEmployeeViewModel>>> Create()
        {
            try
            {
                var result = await _getCreate.GetResponse<GetCreateEmployeeResponse>(new GetCreateEmployeeRequest());
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/getCreate", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<CreateEmployeeViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/getCreate", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<NotificationViewModel>> Create(CreateEmployeeViewModel request)
        {
            if (!ModelState.IsValid)
            {
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            }
            try
            {
                var result = await _create.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/create", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/create", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpGet]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel<UpdateEmployeeViewModel>>> Update(int id)
        {
            if (id == 0)
            {
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));
            }
            try
            {
                var result = await _getUpdate.GetResponse<GetUpdateEmployeeResponse>(new GetUpdateEmployeeRequest { EmployeeId = id });
                if (result.Message.Notification.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/getUpdate", result.Message.Notification.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message.Notification);
                }
                return Ok(new NotificationViewModel<UpdateEmployeeViewModel>(result.Message.Model));
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/getUpdate", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<NotificationViewModel>> Update(UpdateEmployeeViewModel request)
        {
            if (!ModelState.IsValid)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.DataNotValid }));

            try
            {
                var result = await _update.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/update", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(result.Message);
                }
                var user = HttpContext.User?.Identity?.Name;
                var updateApp = new UpdateEmployeeFullNameRequest()
                {
                    EmployeeId = request.Id,
                    IniciatorId = request.Id,
                    EmployeeFullName = $"{request.Surname} {request.Name} {request.Patronymic}", 
                    IniciatorFullName = $"{request.Surname} {request.Name} {request.Patronymic}", 
                    Contact = request.WorkTelephone != null ? request.WorkTelephone : request.MobileTelephone
                };
                //if (!string.IsNullOrEmpty(user))
                //{
                //    var iniciator = await _getByUserName.GetResponse<EmployeeByIdResponse>(new GetEmployeeByUserNameRequest { UserName = user });
                //    if(iniciator.Message.Notification.Type == NotificationType.Success)
                //    {
                //        updateApp.Contact = iniciator.Message.Model.WorkTelephone;
                //        updateApp.IniciatorId = iniciator.Message.Model.Id;
                //        updateApp.IniciatorFullName = $"{iniciator.Message.Model.Surname} {iniciator.Message.Model.Name} {iniciator.Message.Model.Patronymic}"; 
                //    }
                //}
                //else
                //{
                //    var message = new CreateLog(AddressConst.Employee + "/update", $"Не удалось отправить данные для изменения заявки || Нет инициатора || UserName = null || Для выполнения операции нужен авторизованный пользователь", NotificationType.Error, HttpContext.User?.Identity?.Name);
                //    await _publishEndpoint.Publish(message);
                //}

                await _publishEndpoint.Publish(updateApp);
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/update", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpPut]
        [Route("updatePhoto")]
        public async Task<ActionResult<NotificationViewModel>> UpdatePhoto(CreateOrEditEmployeePhotoViewModel request)
        {
            if (request.EmployeeId == 0 || request.Photo == null)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            var ext = request.Photo.FileName.Split('.')[1];

            if (ext != "jpeg" && ext != "jpg" && ext != "psd" && ext != "bmp" && ext != "gif" && ext != "png" &&
                ext != "ico" &&
                ext != "JPEG" && ext != "JPG" && ext != "PSD" && ext != "BMP" && ext != "GIF" && ext != "PNG" &&
                ext != "ICO")
            {
                var message = new CreateLog(AddressConst.Employee + "/updatePhoto",
                    $"Неверный формат файла || Модель: < {typeof(IFormFile)} > || Расширение файла: < {ext} > || Название файла: < {request.Photo.FileName} >",
                    NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.ErrorFileExtension }));
            }

            using (var binaryReader = new BinaryReader(request.Photo.OpenReadStream()))
                request.BytesPhoto = binaryReader.ReadBytes((int)request.Photo.Length);
            request.Photo = null;

            try
            {
                var result = await _updatePhoto.GetResponse<NotificationViewModel>(request);
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/updatePhoto", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/updatePhoto", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
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
                var exist = await _existDependency.GetResponse<ExistDepandencyEntityResponse>(new ExistDependenceEntityRequest { EmployeeId = id });
                if (!exist.Message.Exists)
                {
                    var message = new CreateLog(AddressConst.Employee + "/delete",
                        $"Ошибка при удалении || Модель: < {typeof(EmployeeViewModel)} > || ID: < {id} > || Сотрудник привязан к заявке",
                        NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                    return Ok(new NotificationViewModel(new[] { TypeOfErrors.DeletionEntityError }));
                }

                var result = await _delete.GetResponse<NotificationViewModel>(new DeleteEmployeeRequest { EmployeeId = id });
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/delete", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/delete", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }

        [HttpDelete]
        [Route("deletePhoto")]
        public async Task<ActionResult<NotificationViewModel>> DeletePhoto(int id)
        {
            if (id == 0)
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.BadRequest }));

            try
            {
                var result = await _deletePhoto.GetResponse<NotificationViewModel>(new DeleteEmployeePhotoRequest { EmployeeId = id });
                if (result.Message.Type != NotificationType.Success)
                {
                    var message = new CreateLog(AddressConst.Employee + "/deletePhoto", result.Message.AspNetException, NotificationType.Error, HttpContext.User?.Identity?.Name);
                    await _publishEndpoint.Publish(message);
                }
                return Ok(result.Message);
            }
            catch (Exception e)
            {
                var message = new CreateLog(AddressConst.Employee + "/deletePhoto", e.Message, NotificationType.Error, HttpContext.User?.Identity?.Name);
                await _publishEndpoint.Publish(message);
                return Ok(new NotificationViewModel(new[] { TypeOfErrors.OrganizationEntityError }, type: NotificationType.Warn));
            }
        }
    }
}
