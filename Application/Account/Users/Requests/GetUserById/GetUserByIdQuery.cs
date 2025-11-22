using Mediator;

namespace Application.Account.Users.Requests.GetUserById
{
    public class GetUserByIdQuery : IRequest<UserProfile?>
    {
        public string Id { get; }

        public GetUserByIdQuery(string id)
        {
            Id = id;
        }
    }
}
