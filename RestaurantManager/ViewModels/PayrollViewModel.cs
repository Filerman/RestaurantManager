using System;

namespace RestaurantManager.ViewModels
{
    public class PayrollViewModel
    {
        public string EmployeeName { get; set; }
        public string EmployeeEmail { get; set; }
        public double TotalHours { get; set; }
        public decimal TotalSalary { get; set; }
        public int ShiftsCount { get; set; }
    }

    public class PayrollIndexViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public List<PayrollViewModel> Payrolls { get; set; }
    }
}