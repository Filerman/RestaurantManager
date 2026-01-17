using System;

namespace RestaurantManager.ViewModels
{
    public class EmployeeAvailabilityStatusViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool HasProvidedAvailability { get; set; } 
    }

    public class AvailabilityManageViewModel
    {
        public int NextMonth { get; set; }
        public int NextMonthYear { get; set; }
        public bool IsUrgent { get; set; } 
        public List<EmployeeAvailabilityStatusViewModel> EmployeesStatus { get; set; }
    }
}