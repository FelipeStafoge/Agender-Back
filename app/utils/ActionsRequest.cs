public static class ActionsRequest
{
    public static class Error
    {
        public static class Register
        {
            public const string UserAlreadyExists = "user_already_exists";
        }

        public static class Login
        {
            public const string WrongPasswordOrEmail = "wrong_password_or_email";
        }
    }

    public static class Success
    {
        public static class Register
        {
            public const string UserRegistered = "user_registered";
        }

        public static class Login
        {
            public const string UserLoggedIn = "user_logged_in";
        }
    }
}