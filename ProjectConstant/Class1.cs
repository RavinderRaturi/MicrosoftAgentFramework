namespace ProjectConstant
{
    public static class UserSecretsKey
    {
        // Key used to fetch the Azure OpenAI API key from User Secrets.
        public const string ApiKeys_AzureOpenAI = "ApiKeys:AzureOpenAI";

        // Hardcoded Azure OpenAI endpoint used by the client.
        public const string ApiKeys_AzureOpenAIEndPoint = "https://rr-af.openai.azure.com/";

        // Model name used for Azure OpenAI operations.
        public const string ApiKeys_AzureOpenAIModel = "gpt-5-mini";

        // Key used to fetch the OpenAI API key from User Secrets.
        public const string ApiKeys_OpenAI = "ApiKeys:OpenAI";

        // Model name used for standard OpenAI operations.
        public const string ApiKeys_OpenAIModel = "gpt-5-nano";
    }




    public static class Banner
    {public const string MainBanner = @"
                      /\                          /\
                     /  \        /\    /\        /  \
            /\      /----\  /\  /  \  /  \  /\   /----\
           /  \    /      \/  \/    \/    \/  \ /      \
     /\   /----\  /                       .    \----\   \
    /  \_/      \/   THE HIMALAYAN IBEX   / \       \___/
   /     \      /                         \_/          
  /       \____/                                      
";
    }
}
