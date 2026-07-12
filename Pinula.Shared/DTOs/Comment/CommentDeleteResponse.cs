namespace Pinula.Shared.DTOs
{
    public class CommentDeleteResponse
    {
        public decimal NewAverageRating { get; set; }
        public int NewUsersRatedCount { get; set; }
        public bool UserAlreadyRated { get; set; }
    }
}
