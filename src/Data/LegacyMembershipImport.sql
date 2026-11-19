-- Optional one-off script: imports SimpleMembership users into ASP.NET Core Identity.
-- Run against the database that holds BOTH the legacy tables (UserProfile, webpages_Membership)
-- and the new Identity tables (create the latter with: dotnet ef database update --context ApplicationDbContext).
--
-- SimpleMembership hashes (Base64 of 0x00 + 16-byte salt + 32-byte PBKDF2-HMACSHA1 key, 1000 iterations)
-- are verified natively by ASP.NET Core Identity's PasswordHasher and transparently re-hashed to the
-- current format on the first successful login, so no password reset is required.
INSERT INTO dbo.AspNetUsers
    (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash,
     SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
SELECT CONVERT(nvarchar(450), NEWID()),
       p.UserName,
       UPPER(p.UserName),
       NULL, NULL, 0,
       m.Password,
       CONVERT(nvarchar(36), NEWID()),
       CONVERT(nvarchar(36), NEWID()),
       0, 0, 0, 0
FROM dbo.UserProfile p
LEFT JOIN dbo.webpages_Membership m ON m.UserId = p.UserId
WHERE NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers u WHERE u.NormalizedUserName = UPPER(p.UserName));

-- External (OAuth) logins can be mapped from dbo.webpages_OAuthMembership (Provider, ProviderUserId, UserId)
-- into dbo.AspNetUserLogins (LoginProvider, ProviderKey, ProviderDisplayName, UserId) when the provider keys
-- are still valid for the new OAuth 2.0 handlers (Google OpenID 2.0 identifiers cannot be mapped).
