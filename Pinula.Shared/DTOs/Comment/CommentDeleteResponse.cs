namespace Pinula.Shared.DTOs
{
    public class CommentDeleteResponse : GeneralResponse
    {
        public decimal NewAverageRating { get; set; }
        public int NewUsersRatedCount { get; set; }
        public bool UserAlreadyRated { get; set; }
    }
}