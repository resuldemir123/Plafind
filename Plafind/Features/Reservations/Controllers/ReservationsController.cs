using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plafind.Features.Reservations.Services;
using Plafind.Features.Reservations.ViewModels;
using Plafind.Features.Reservations.Mappings;
using Plafind.Data;
using Plafind.Models;
using AutoMapper;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Plafind.Features.Reservations.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly IReservationService _reservationService;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ReservationsController(
            IReservationService reservationService,
            ApplicationDbContext context,
            IMapper mapper)
        {
            _reservationService = reservationService;
            _context = context;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            var reservations = await _reservationService.GetUserReservationsAsync(userId);
            return View(reservations);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int businessId)
        {
            // Giriş yapmamış kullanıcıları login sayfasına yönlendir
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Reservations", new { businessId }) });
            }

            if (User.IsInRole("Admin") || User.IsInRole("BusinessOwner"))
            {
                return Forbid();
            }

            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null) return NotFound();

            var viewModel = new CreateReservationViewModel
            {
                BusinessId = businessId
            };

            ViewBag.Business = business;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReservationViewModel viewModel)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("Admin") || User.IsInRole("BusinessOwner"))
            {
                return Forbid();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses.FindAsync(viewModel.BusinessId);
            if (business == null)
            {
                ModelState.AddModelError("", "Geçersiz işletme ID'si.");
                ViewBag.Business = null;
                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Business = business;
                return View(viewModel);
            }

            var reservation = _mapper.Map<Reservation>(viewModel);
            await _reservationService.CreateReservationAsync(reservation, userId);

            TempData["SuccessMessage"] = "Rezervasyon talebiniz alındı.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);
            if (reservation == null)
                return NotFound();

            return View(reservation);
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var canCancel = await _reservationService.CanUserCancelReservationAsync(id, userId);
            if (!canCancel)
            {
                return Json(new { success = false, message = "Bu rezervasyon iptal edilemez" });
            }

            var result = await _reservationService.CancelReservationAsync(id, userId);
            if (!result)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            return Json(new { success = true, message = "Rezervasyon iptal edildi" });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var reservations = await _reservationService.GetAllReservationsAsync();
            return View(reservations);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                await _reservationService.ApproveReservationAsync(id);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(AdminIndex));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                await _reservationService.RejectReservationAsync(id);
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}

