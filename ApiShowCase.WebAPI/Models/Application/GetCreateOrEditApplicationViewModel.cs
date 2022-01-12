using System.Collections.Generic;
using System.Linq;
using ApplicationContracts.ViewModels.Application.ApplicationToItModels;
using ApplicationContracts.ViewModels.Application.Priority;
using ApplicationContracts.ViewModels.Application.StateModels;
using ApplicationContracts.ViewModels.Application.TypeModels;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.Application;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.DepartmentViewModels;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.EmployeeViewModels;
using OrganizationEntityContracts.ViewModels.OrgEntityViewModel.RoomViewModels;

namespace ApiShowCase.WebAPI.Models.Application
{
    public class GetCreateOrEditApplicationViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string Note { get; set; }
        public string Contact { get; set; }
        public int ApplicationTypeId { get; set; }
        public int ApplicationPriorityId { get; set; }
        public int DepartmentId { get; set; }
        public int RoomId { get; set; }
        public int EmployeeId { get; set; }
        public int StateId { get; set; }

        public ApplicationPriorityViewModel Priority { get; set; }
        public ApplicationStateViewModel State { get; set; }
        public ApplicationTypeViewModel Type { get; set; }
        public EmployeeViewModel Employee { get; set; }
        public DepartmentViewModel Department { get; set; }
        public RoomViewModel Room { get; set; }


        public string DepartamentName { get; set; }
        public string RoomName { get; set; }
        public string EmployeeFullName { get; set; }

        public List<ApplicationPriorityViewModel> SelectPriority { get; set; }
        public List<ApplicationTypeViewModel> SelectType { get; set; }
        public List<ApplicationStateViewModel> SelectState { get; set; }
        public List<DepartmentViewModel> SelectDepartment { get; set; }
        public List<RoomViewModel> SelectRoom { get; set; }
        public List<EmployeeViewModel> SelectEmployee { get; set; }

        public GetCreateOrEditApplicationViewModel() { }

        public GetCreateOrEditApplicationViewModel(CreateApplicationToITViewModel app, GetCreateForApplicationResponse dependency)
        {
            SelectPriority = app.SelectPriority;
            SelectType = app.SelectType;
            SelectState = app.SelectState;
            SelectDepartment = dependency.SelectDepartment;
            SelectRoom = dependency.SelectRoom;
            SelectEmployee = dependency.SelectEmployee;
        }
        public GetCreateOrEditApplicationViewModel(UpdateApplicationViewModel app, GetCreateForApplicationResponse dependency)
        {
            Id = app.Id;
            Name = app.Name;
            Content = app.Content;
            Contact = app.Contact;
            Note = app.Note;
            ApplicationPriorityId = app.ApplicationPriorityId;
            ApplicationTypeId = app.ApplicationTypeId;
            DepartmentId = app.DepartamentId;
            RoomId = app.RoomId;
            EmployeeId = app.EmployeeId;
            StateId = app.StateId;
            SelectPriority = app.SelectPriority;
            SelectType = app.SelectType;
            SelectState = app.SelectState;
            SelectDepartment = dependency.SelectDepartment;
            SelectRoom = dependency.SelectRoom;
            SelectEmployee = dependency.SelectEmployee;

            Priority = app.Priority;
            Type = app.Type;
            State = app.State;

            EmployeeFullName = app.EmployeeFullName;
            DepartamentName = app.DepartamentName;
            RoomName = app.RoomName;

            Department = SelectDepartment.FirstOrDefault(x => x.Id == DepartmentId);
            Employee = SelectEmployee.FirstOrDefault(x => x.Id == EmployeeId);
            Room = SelectRoom.FirstOrDefault(x => x.Id == RoomId);
        }
    }
}