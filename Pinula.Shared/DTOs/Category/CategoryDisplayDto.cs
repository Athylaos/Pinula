namespace Pinula.Shared.DTOs
{
    public class CategoryDisplayDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string PictureUrl { get; set; } = "default_category_picture.png";

        public short SortOrder { get; set; }

        public Guid? ParentCategoryId { get; set; }

        public virtual ICollection<CategoryDisplayDto> ChildCategories { get; set; } = new List<CategoryDisplayDto>();
        public virtual CategoryDisplayDto? ParentCategory { get; set; }
    }
}
