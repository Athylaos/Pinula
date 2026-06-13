namespace Pinula.Shared.DTOs
{
    public class DeleteCommentResponse
    {
        public decimal NewAverageRating { get; set; }
        public int NewUsersRatedCount { get; set; }
        public bool UserAlreadyRated { get; set; }
    }
}
