
namespace GenericFilters
{
    public class FilterOptions
    {
        /// <summary>
        /// If true, do not throw any exception if Filter property decorated with FilterMemberAttribute
        /// is missing in the Model class
        /// </summary>
        public bool Optimistic { get; set; } = false;
    }
}
