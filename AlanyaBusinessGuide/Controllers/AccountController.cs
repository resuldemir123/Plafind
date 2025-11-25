using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AlanyaBusinessGuide.Models;
using AlanyaBusinessGuide.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace AlanyaBusinessGuide.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ISmsService _smsService;
        private readonly IMemoryCache _cache;

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            RoleManager<IdentityRole> roleManager,
            ISmsService smsService,
            IMemoryCache cache)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Model binding can produce null model; guard against it
            if (model == null)
            {
                ModelState.AddModelError(string.Empty, "Geçersiz istek.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(string.Empty, "E-posta ve şifre gereklidir.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model == null)
            {
                ModelState.AddModelError(string.Empty, "Geçersiz istek.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(string.Empty, "E-posta ve şifre gereklidir.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                // Onay kontrolü
                if (!model.ConsentAccepted)
                {
                    ModelState.AddModelError(nameof(model.ConsentAccepted), "Kullanım şartlarını kabul etmeniz gerekmektedir.");
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Country = model.Country?.Trim(),
                    ConsentAccepted = model.ConsentAccepted,
                    ConsentDate = DateTime.Now
                };
                var result = await _userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    if (await _roleManager.RoleExistsAsync("User"))
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                    }
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Harici sağlayıcı hatası: {remoteError}";
                return RedirectToAction("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Harici giriş bilgileri yüklenemedi.";
                return RedirectToAction("Login");
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            
            if (signInResult.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }

            if (signInResult.IsLockedOut)
            {
                TempData["ErrorMessage"] = "Hesabınız kilitlenmiş.";
                return RedirectToAction("Login");
            }

            // Kullanıcı yoksa oluştur
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "E-posta adresi alınamadı.";
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = name,
                    FullName = name,
                    CreatedDate = DateTime.Now,
                    ConsentAccepted = true,
                    ConsentDate = DateTime.Now
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
                    return RedirectToAction("Login");
                }

                if (await _roleManager.RoleExistsAsync("User"))
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                return RedirectToAction("Login");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToLocal(returnUrl);
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Profile()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }
            ViewBag.IsAdmin = User.IsInRole("Admin");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Telefon numarasını temizle
                var cleanPhone = model.PhoneNumber.Replace(" ", "")
                                                  .Replace("-", "")
                                                  .Replace("(", "")
                                                  .Replace(")", "")
                                                  .Replace("+", "");

                if (!cleanPhone.StartsWith("90") && cleanPhone.Length == 10)
                {
                    cleanPhone = "90" + cleanPhone;
                }

                // Kullanıcıyı telefon numarasına göre bul
                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == cleanPhone || 
                                             u.PhoneNumber == model.PhoneNumber ||
                                             u.PhoneNumber!.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone);

                if (user == null)
                {
                    // Güvenlik için: Kullanıcı bulunamasa bile başarı mesajı göster
                    TempData["SuccessMessage"] = "Eğer bu telefon numarasına kayıtlı bir hesap varsa, şifre sıfırlama kodu SMS ile gönderildi.";
                    return RedirectToAction(nameof(ResetPassword));
                }

                // 6 haneli rastgele kod oluştur
                var random = new Random();
                var code = random.Next(100000, 999999).ToString();

                // Kodu cache'e kaydet (10 dakika geçerli)
                var cacheKey = $"PasswordResetCode_{cleanPhone}";
                _cache.Set(cacheKey, code, TimeSpan.FromMinutes(10));

                // SMS gönder
                var smsSent = await _smsService.SendPasswordResetCodeAsync(cleanPhone, code);

                if (smsSent)
                {
                    TempData["SuccessMessage"] = "Şifre sıfırlama kodu telefon numaranıza SMS ile gönderildi.";
                    TempData["PhoneNumber"] = cleanPhone;
                    return RedirectToAction(nameof(ResetPassword));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "SMS gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }

            return View(model);
        }

        public IActionResult ResetPassword(string? userId = null, string? token = null)
        {
            var model = new ResetPasswordViewModel();
            
            // E-posta linkinden geliyorsa
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(token))
            {
                model.UserId = userId;
                model.Token = token;
                return View(model);
            }
            
            // SMS kodundan geliyorsa
            if (TempData["PhoneNumber"] != null)
            {
                model.PhoneNumber = TempData["PhoneNumber"].ToString();
            }
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                ApplicationUser? user = null;
                string? resetToken = null;

                // E-posta token'ı ile mi geliyor?
                if (!string.IsNullOrEmpty(model.UserId) && !string.IsNullOrEmpty(model.Token))
                {
                    user = await _userManager.FindByIdAsync(model.UserId);
                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı.");
                        return View(model);
                    }
                    resetToken = model.Token;
                }
                // SMS kodu ile mi geliyor?
                else if (!string.IsNullOrEmpty(model.PhoneNumber) && !string.IsNullOrEmpty(model.Code))
                {
                    // Telefon numarasını temizle
                    var cleanPhone = model.PhoneNumber.Replace(" ", "")
                                                      .Replace("-", "")
                                                      .Replace("(", "")
                                                      .Replace(")", "")
                                                      .Replace("+", "");

                    if (!cleanPhone.StartsWith("90") && cleanPhone.Length == 10)
                    {
                        cleanPhone = "90" + cleanPhone;
                    }

                    // Cache'den kodu kontrol et
                    var cacheKey = $"PasswordResetCode_{cleanPhone}";
                    if (!_cache.TryGetValue(cacheKey, out string? cachedCode) || cachedCode != model.Code)
                    {
                        ModelState.AddModelError(string.Empty, "Geçersiz veya süresi dolmuş doğrulama kodu.");
                        return View(model);
                    }

                    // Kullanıcıyı bul
                    user = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == cleanPhone || 
                                                 u.PhoneNumber == model.PhoneNumber ||
                                                 u.PhoneNumber!.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone);

                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, "Bu telefon numarasına kayıtlı kullanıcı bulunamadı.");
                        return View(model);
                    }

                    resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    
                    // Cache'den kodu sil
                    _cache.Remove(cacheKey);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz istek.");
                    return View(model);
                }

                if (user == null || string.IsNullOrEmpty(resetToken))
                {
                    ModelState.AddModelError(string.Empty, "Geçersiz istek.");
                    return View(model);
                }

                // Şifreyi sıfırla
                var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz.";
                    return RedirectToAction(nameof(Login));
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }

            return View(model);
        }
    }
}