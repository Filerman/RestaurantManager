using RestaurantManager.Models;
using System.Collections.Generic;

namespace RestaurantManager.ViewModels
{
    public class ManageTableViewModel
    {
        public Table Table { get; set; }
        public Order? CurrentOrder { get; set; }
        public List<string> Categories { get; set; }
        public List<MenuItem> MenuItems { get; set; }
    }
}