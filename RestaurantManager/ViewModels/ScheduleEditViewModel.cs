using System;
using System.Collections.Generic;
using RestaurantManager.Models;

namespace RestaurantManager.ViewModels
{
    // ViewModel dla pojedynczej zmiany w edytorze
    public class ShiftViewModel
    {
        public int Id { get; set; } // 0 dla nowej zmiany
        public int ScheduleId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int? PositionTagId { get; set; } // Wymagany tag
        public int? AssignedUserId { get; set; } // Przypisany pracownik (ID)
        public string? AssignedUserName { get; set; } // Do wyświetlania

        // Do walidacji i filtrowania
        public string? PositionTagName { get; set; }
        public bool IsMarkedForDeletion { get; set; } = false;
    }

    // ViewModel dla pojedynczego dnia w edytorze grafiku
    public class ScheduleDayViewModel
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; }
        public List<ShiftViewModel> Shifts { get; set; } = new List<ShiftViewModel>();
    }

    // Główny ViewModel dla widoku edycji grafiku
    public class ScheduleEditViewModel
    {
        public int Id { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPublished { get; set; }

        // Lista dni w grafiku, każdy z listą swoich zmian
        public List<ScheduleDayViewModel> Days { get; set; } = new List<ScheduleDayViewModel>();

        // Dane pomocnicze dla dropdownów w formularzu dodawania zmiany
        public List<User>? AvailableEmployees { get; set; } // Lista pracowników do wyboru
        public List<PositionTag>? AvailableTags { get; set; } // Lista tagów do wyboru
    }
}