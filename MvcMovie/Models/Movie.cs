using System.ComponentModel.DataAnnotations;
namespace MvcMovie.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }
        public string? Title { get; set;}
        public DateTime ReleaseDate {get; set;}
        public string? Genre {get; set;}
        public float Price {get; set;}


    }
}