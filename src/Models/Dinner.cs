using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NetTopologySuite.Geometries;

namespace NerdDinner.Models
{
    public class Dinner
    {
        [HiddenInput(DisplayValue = false)]
        public int DinnerID { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(50, ErrorMessage = "Title may not be longer than 50 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Event Date is required")]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(256, ErrorMessage = "Description may not be longer than 256 characters")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [StringLength(20, ErrorMessage = "Hosted By name may not be longer than 20 characters")]
        [Display(Name = "Host's Name")]
        public string HostedBy { get; set; }

        [Required(ErrorMessage = "Contact info is required")]
        [StringLength(20, ErrorMessage = "Contact info may not be longer than 20 characters")]
        [Display(Name = "Contact Info")]
        public string ContactPhone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(50, ErrorMessage = "Address may not be longer than 50 characters")]
        [Display(Name = "Address, City, State, ZIP")]
        public string Address { get; set; }

        [UIHint("CountryDropDown")]
        public string Country { get; set; }

        /// <summary>
        /// Geographic location (SRID 4326). X is the longitude and Y is the latitude.
        /// </summary>
        [Display(Name = "Location")]
        [Column(TypeName = "geography")]
        [ValidateNever]
        public Point Location { get; set; }

        public virtual ICollection<RSVP> RSVPs { get; set; } = new List<RSVP>();

        public bool IsHostedBy(string userName)
        {
            return String.Equals(HostedBy, userName, StringComparison.Ordinal);
        }

        public bool IsUserRegistered(string userName)
        {
            return RSVPs != null && RSVPs.Any(r => r.AttendeeName == userName);
        }

        [UIHint("LocationDetail")]
        [NotMapped]
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        [ValidateNever]
        public LocationDetail LocationDetail
        {
            get
            {
                return new LocationDetail() { Location = this.Location, Id = this.DinnerID, Title = this.Title, Address = this.Address };
            }
        }
    }

    public class LocationDetail
    {
        public Point Location { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
    }
}
