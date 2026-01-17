using System;
using System.Collections.Generic;
using RestaurantManager.Models;

namespace RestaurantManager.ViewModels
{
    public class ShiftViewModel
    {
        public int Id { get; set; } 
        public int ScheduleId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int? PositionTagId { get; set; }
        public int? AssignedUserId { get; set; } 
        public string? AssignedUserName { get; set; }

        public string? PositionTagName { get; set; }
        public bool IsMarkedForDeletion { get; set; } = false;
    }

    public class ScheduleDayViewModel
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; }
        public List<ShiftViewModel> Shifts { get; set; } = new List<ShiftViewModel>();
    }

    public class ScheduleEditViewModel
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPublished { get; set; }

        public List<ScheduleDayViewModel> Days { get; set; } = new List<ScheduleDayViewModel>();

        public List<User>? AvailableEmployees { get; set; }
        public List<PositionTag>? AvailableTags { get; set; }
    }
}