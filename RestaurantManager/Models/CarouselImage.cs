using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantManager.Models
{
    public class CarouselImage
    {
        public int Id { get; set; }
        public string ImagePath { get; set; }

        public int HomePageSettingId { get; set; }

        [ForeignKey("HomePageSettingId")]
        public virtual HomePageSetting HomePageSetting { get; set; }
    }
}