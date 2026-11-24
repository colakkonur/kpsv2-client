// KpsV2.Client'ı bir ASP.NET Core projesine bağlamanın en kısa örneği.
//
//   cd samples/KpsV2.Sample.WebApi && dotnet run
//   curl -s localhost:5080/kps/person -H 'Content-Type: application/json' -d '{"identityNumber":10774881040}'
//
// Bu örnek yalnızca Test ortamına, herkese açık test kullanıcısıyla bağlanır (appsettings*.json).
// Kendi projenizde aynı "Kps" bölümüne kendi Prod bilgilerinizi girersiniz; bkz. docs/kullanim.md.

using KpsV2;
using KpsV2.Contracts;

var builder = WebApplication.CreateBuilder(args);

// 1) Tek satır: ayarlar "Kps" bölümünden okunur, IKpsClient singleton olarak kaydedilir.
builder.Services.AddKpsClient(builder.Configuration.GetSection(KpsOptions.SectionName));
builder.Services.AddProblemDetails();

var app = builder.Build();

// 2) KpsException'ı anlamlı HTTP cevaplarına çevir. Kimlik no / kişi bilgisi cevaba ve loglara yazılmaz.
app.Use(async (ctx, next) =>
{
    try
    {
        await next(ctx);
    }
    catch (KpsException ex)
    {
        var (durum, baslik) = ex.Kind switch
        {
            KpsErrorKind.NotFound => (StatusCodes.Status404NotFound, "Kayıt bulunamadı"),
            KpsErrorKind.Unauthorized => (StatusCodes.Status403Forbidden, "Kurumun bu KPS metoduna yetkisi yok"),
            KpsErrorKind.Authentication or KpsErrorKind.Configuration => (StatusCodes.Status502BadGateway, "KPS kimlik doğrulaması/ayarları hatalı"),
            _ when ex.IsTransient => (StatusCodes.Status503ServiceUnavailable, "KPS şu an yanıt vermiyor, tekrar deneyin"),
            _ => (StatusCodes.Status502BadGateway, "KPS hatası"),
        };
        app.Logger.LogWarning("KPS hatası: {Kind} {Soap}", ex.Kind, ex.SoapFault);
        await Results.Problem(title: baslik, detail: ex.SoapFault, statusCode: durum,
            extensions: new Dictionary<string, object?> { ["kpsErrorKind"] = ex.Kind.ToString() }).ExecuteAsync(ctx);
    }
    catch (ArgumentException ex)
    {
        // Geçersiz kimlik numarası vb.: istek KPS'ye hiç gönderilmedi.
        await Results.Problem(title: "Geçersiz istek", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(ctx);
    }
});

var kps = app.MapGroup("/kps");

// Kimlik numarası URL'de değil gövdede taşınır: erişim loglarına kişisel veri düşmesin.

// Özet: üç kimlik türü (TC / yabancı / mavi kart) tek tipte.
kps.MapPost("/person", async (IdentityRequest r, IKpsClient k, CancellationToken ct) =>
{
    var s = await k.BilesikKisiSorgulaAsync(r.IdentityNumber, ct);
    var ozet = s.GetDataOrThrow().ToSummary();
    return ozet is null ? Results.NotFound() : Results.Ok(new
    {
        ozet.Type, ozet.IdentityNumber, ozet.FirstName, ozet.LastName, ozet.FatherName, ozet.MotherName,
        Gender = ozet.Gender?.Aciklama,
        BirthDate = ozet.BirthDate?.Date,
        ozet.BirthPlace,
        Status = ozet.Status?.Aciklama,
        ozet.IsDeceased,
    });
});

// Tam cevap: WSDL'deki tüm alanlar (kayıt yeri, cüzdan, TCKK, izin bilgileri...).
kps.MapPost("/person/full", async (IdentityRequest r, IKpsClient k, CancellationToken ct) =>
    Results.Ok((await k.BilesikKisiSorgulaAsync(r.IdentityNumber, ct)).GetDataOrThrow()));

kps.MapPost("/residence", async (IdentityRequest r, IKpsClient k, CancellationToken ct) =>
{
    var s = await k.KimlikNoIleYerlesimYeriSorgulaAsync(r.IdentityNumber, ct);
    var adres = s.GetDataOrThrow().YerlesimYeriAdresi;
    return Results.Ok(new { Address = adres?.AcikAdres, AddressType = adres?.AdresTip?.Aciklama, AddressNumber = adres?.AdresNo });
});

kps.MapPost("/family", async (FamilyRequest r, IKpsClient k, CancellationToken ct) =>
{
    var kriter = KpsCriteria.Kinship(r.IdentityNumber, r.BirthDate);
    var s = r.IncludeSiblings
        ? await k.KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(kriter, ct)
        : await k.KimlikNoIleVatandasAileBilgisiSorgulaAsync(kriter, ct);
    return Results.Ok(s.GetDataOrThrow());
});

kps.MapPost("/identity-number", async (PersonRequest r, IKpsClient k, CancellationToken ct) =>
{
    var s = await k.TcKimlikNoSorgulaAsync(
        KpsCriteria.Person(r.FirstName, r.LastName, r.FatherName, r.MotherName, r.Gender, r.BirthDate, r.BirthPlace), ct);
    return Results.Ok(new { IdentityNumber = s.GetDataOrThrow().TCKimlikNo });
});

// Kurumun hangi metotlara ve alanlara yetkisi var?
kps.MapGet("/permissions", async (IKpsClient k, CancellationToken ct) =>
{
    var s = await k.YetkiListesiAsync(ct);
    return Results.Ok(s.GetDataOrThrow().MethodPermissions().Where(y => y.Service == KpsPermissionExtensions.KpsServiceName));
});

kps.MapGet("/permissions/{method}/fields", async (string method, IKpsClient k, CancellationToken ct) =>
    Results.Ok((await k.YetkiListesiAsync(ct)).GetDataOrThrow().FieldPermissions(method)));

app.Run();

internal sealed record IdentityRequest(long IdentityNumber);
internal sealed record FamilyRequest(long IdentityNumber, DateOnly BirthDate, bool IncludeSiblings = false);
internal sealed record PersonRequest(string FirstName, string LastName, string FatherName, string MotherName, ECinsiyet Gender, DateOnly BirthDate, string BirthPlace);
