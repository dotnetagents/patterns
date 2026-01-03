namespace DotNetAgents.Patterns.Routing.UseCases.CustomerSupport;

/// <summary>
/// Sample support tickets for testing and benchmarking.
/// </summary>
public static class SampleTickets
{
    /// <summary>
    /// Billing-related ticket (duplicate charge).
    /// </summary>
    public const string Billing = """
        Subject: Duplicate charge on my account

        Hi, I was charged twice for my monthly subscription last week. I can see
        two charges of $49.99 on my credit card statement dated December 28th.
        My account number is AC-12345. Can you please refund the duplicate charge?
        This is urgent as I need the money back before the new year.

        Thanks,
        John
        """;

    /// <summary>
    /// Technical support ticket (API error).
    /// </summary>
    public const string Technical = """
        Subject: 403 Forbidden error on API calls

        I'm getting a 403 Forbidden error when trying to call your REST API from my
        Node.js application. I've double-checked my API key and it looks correct.
        The endpoint I'm hitting is /api/v2/users and I'm including the
        Authorization header with "Bearer {api_key}". This was working fine
        yesterday but started failing this morning around 9 AM EST.

        Here's the error response:
        {"error": "forbidden", "message": "Access denied"}

        Please help!
        Mike
        """;

    /// <summary>
    /// Account security ticket (password reset).
    /// </summary>
    public const string Account = """
        Subject: Can't reset my password

        I forgot my password and the reset email isn't arriving in my inbox.
        I've checked my spam folder too. My email is sarah.jones@company.com and I
        need to access my account urgently for a presentation tomorrow.
        Is there another way to verify my identity and reset the password?

        I've been a customer for 3 years and have never had this issue before.

        Thanks,
        Sarah
        """;

    /// <summary>
    /// Product inquiry ticket (feature question).
    /// </summary>
    public const string Product = """
        Subject: SSO integration question

        I'm evaluating your software for my team of 50 developers. Can you tell me
        if your Pro plan includes SSO integration with Azure AD? Also, is there
        a way to set up role-based access control for different project areas?
        We need to ensure compliance with our company's security policies.

        Additionally, what's the process for upgrading from Basic to Pro?

        Best regards,
        David Chen
        IT Director
        """;

    /// <summary>
    /// Ambiguous ticket that could be multiple categories.
    /// </summary>
    public const string Ambiguous = """
        Subject: Account upgrade issue

        My account shows I'm on the Basic plan but I upgraded to Pro last week
        and was charged for it. Now I can't access the advanced features that
        should come with Pro. Either I need a refund for the Pro upgrade or
        you need to fix my account to show the correct plan.

        This is really frustrating!
        """;

    /// <summary>
    /// All sample tickets with metadata for testing.
    /// </summary>
    public static IReadOnlyList<(string Name, string Ticket, SupportCategory ExpectedCategory)> All =>
    [
        ("Billing", Billing, SupportCategory.Billing),
        ("Technical", Technical, SupportCategory.Technical),
        ("Account", Account, SupportCategory.Account),
        ("Product", Product, SupportCategory.Product),
        ("Ambiguous", Ambiguous, SupportCategory.Billing), // Primary issue is billing
    ];

    /// <summary>
    /// Default ticket used for benchmark prompt.
    /// </summary>
    public const string DefaultBenchmarkTicket = Technical;
}
