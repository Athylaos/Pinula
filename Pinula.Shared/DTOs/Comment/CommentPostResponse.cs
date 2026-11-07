namespace Pinula.Shared.DTOs
{
    public class CommentPostResponse : GeneralResponse
    {
        public decimal NewAverageRating { get; set; }
        public int NewUsersRatedCount { get; set; }
        public CommentDisplayDto NewComment { get; set; } = new();
        
    }
}