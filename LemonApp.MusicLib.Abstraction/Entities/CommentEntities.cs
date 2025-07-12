namespace LemonApp.MusicLib.Abstraction.Entities;
public record class Comment(string UserName,
                            string UserAvatarUrl,
                            string Content,
                            string LikeCount,
                            string Time,
                            string Id,
                            bool IsLiked);
public record class CommentPageData(List<Comment> PresentComments,
                            List<Comment> HotComments,
                            List<Comment> NewComments);