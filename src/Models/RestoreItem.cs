
namespace AppRestorer
{
    public class RestoreItem
    {
        /// <summary>
        /// Module's full path
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Flag for keep if not found next iteration
        /// </summary>
        public bool Favorite { get; set; }
    }
}
