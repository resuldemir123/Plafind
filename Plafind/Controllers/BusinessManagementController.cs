using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using System.Security.Claims;

namespace Plafind.Controllers
{
    [Authorize(Roles = "Admin,BusinessOwner")]
    public class BusinessManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BusinessManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BusinessManagement - Çoklu işletme yönetimi
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            List<Business> businesses;
            
            if (User.IsInRole("Admin"))
            {
                businesses = await _context.Businesses
                    .Include(b => b.Category)
                    .Include(b => b.Owner)
                    .Include(b => b.Reviews)
                    .Include(b => b.Reservations)
                    .ToListAsync();
            }
            else
            {
                businesses = await _context.Businesses
                    .Where(b => b.OwnerId == userId)
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                    .Include(b => b.Reservations)
                    .ToListAsync();
            }

            // Her işletme için istatistikler
            foreach (var business in businesses)
            {
                business.TotalReviews = await _context.Reviews
                    .CountAsync(r => r.BusinessId == business.Id && r.IsActive && r.IsApproved);
                
                business.AverageRating = await _context.Reviews
                    .Where(r => r.BusinessId == business.Id && r.IsActive && r.IsApproved)
                    .Select(r => (double?)r.Rating)
                    .AverageAsync() ?? 0;
            }

            return View(businesses);
        }

        // GET: BusinessManagement/Branches/5 - Şube yönetimi
        public async Task<IActionResult> Branches(int? businessId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            Business? business = null;
            
            if (businessId.HasValue)
            {
                business = await _context.Businesses
                    .FirstOrDefaultAsync(b => b.Id == businessId.Value);
                
                if (business == null)
                {
                    return NotFound();
                }
                
                // Yetki kontrolü
                if (!User.IsInRole("Admin") && business.OwnerId != userId)
                {
                    return Forbid();
                }
            }
            else
            {
                // İlk işletmeyi seç
                if (User.IsInRole("Admin"))
                {
                    business = await _context.Businesses.FirstOrDefaultAsync();
                }
                else
                {
                    business = await _context.Businesses
                        .FirstOrDefaultAsync(b => b.OwnerId == userId);
                }
            }

            if (business == null)
            {
                return NotFound("İşletme bulunamadı.");
            }

            var branches = await _context.Branches
                .Where(b => b.BusinessId == business.Id)
                .OrderBy(b => b.Name)
                .ToListAsync();

            ViewBag.Business = business;
            ViewBag.BusinessId = business.Id;
            
            // Kullanıcının tüm işletmeleri
            var userBusinesses = User.IsInRole("Admin")
                ? await _context.Businesses.ToListAsync()
                : await _context.Businesses.Where(b => b.OwnerId == userId).ToListAsync();
            ViewBag.UserBusinesses = userBusinesses;

            return View(branches);
        }

        // POST: BusinessManagement/Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBranch(Branch branch)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var business = await _context.Businesses.FindAsync(branch.BusinessId);

            if (business == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && business.OwnerId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                branch.CreatedDate = DateTime.Now;
                _context.Add(branch);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Branches), new { businessId = branch.BusinessId });
            }

            ViewBag.Business = business;
            ViewBag.BusinessId = branch.BusinessId;
            return View("Branches", await _context.Branches.Where(b => b.BusinessId == branch.BusinessId).ToListAsync());
        }

        // POST: BusinessManagement/Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBranch(int id, Branch branch)
        {
            if (id != branch.Id)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingBranch = await _context.Branches
                .Include(b => b.Business)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (existingBranch == null || existingBranch.Business == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && existingBranch.Business.OwnerId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingBranch.Name = branch.Name;
                    existingBranch.Address = branch.Address;
                    existingBranch.Phone = branch.Phone;
                    existingBranch.Email = branch.Email;
                    existingBranch.ManagerName = branch.ManagerName;
                    existingBranch.Latitude = branch.Latitude;
                    existingBranch.Longitude = branch.Longitude;
                    existingBranch.WorkingHours = branch.WorkingHours;
                    existingBranch.IsActive = branch.IsActive;
                    existingBranch.UpdatedDate = DateTime.Now;

                    _context.Update(existingBranch);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BranchExists(branch.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Branches), new { businessId = existingBranch.BusinessId });
            }

            ViewBag.Business = existingBranch.Business;
            ViewBag.BusinessId = existingBranch.BusinessId;
            return View("Branches", await _context.Branches.Where(b => b.BusinessId == existingBranch.BusinessId).ToListAsync());
        }

        // POST: BusinessManagement/Branches/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var branch = await _context.Branches
                .Include(b => b.Business)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && branch.Business?.OwnerId != userId)
            {
                return Forbid();
            }

            var businessId = branch.BusinessId;
            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Branches), new { businessId });
        }

        // GET: BusinessManagement/Employees/5 - Çalışan yönetimi
        public async Task<IActionResult> Employees(int? businessId, int? branchId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            Business? business = null;
            
            if (businessId.HasValue)
            {
                business = await _context.Businesses.FindAsync(businessId.Value);
                
                if (business == null)
                {
                    return NotFound();
                }
                
                if (!User.IsInRole("Admin") && business.OwnerId != userId)
                {
                    return Forbid();
                }
            }
            else
            {
                if (User.IsInRole("Admin"))
                {
                    business = await _context.Businesses.FirstOrDefaultAsync();
                }
                else
                {
                    business = await _context.Businesses
                        .FirstOrDefaultAsync(b => b.OwnerId == userId);
                }
            }

            if (business == null)
            {
                return NotFound("İşletme bulunamadı.");
            }

            IQueryable<Employee> employeesQuery = _context.Employees
                .Where(e => e.BusinessId == business.Id);

            if (branchId.HasValue)
            {
                employeesQuery = employeesQuery.Where(e => e.BranchId == branchId);
            }

            var employees = await employeesQuery
                .Include(e => e.Branch)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            var branches = await _context.Branches
                .Where(b => b.BusinessId == business.Id && b.IsActive)
                .ToListAsync();

            ViewBag.Business = business;
            ViewBag.BusinessId = business.Id;
            ViewBag.Branches = branches;
            ViewBag.SelectedBranchId = branchId;
            
            var userBusinesses = User.IsInRole("Admin")
                ? await _context.Businesses.ToListAsync()
                : await _context.Businesses.Where(b => b.OwnerId == userId).ToListAsync();
            ViewBag.UserBusinesses = userBusinesses;

            return View(employees);
        }

        // POST: BusinessManagement/Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(Employee employee)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var business = await _context.Businesses.FindAsync(employee.BusinessId);

            if (business == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && business.OwnerId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                employee.CreatedDate = DateTime.Now;
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Employees), new { businessId = employee.BusinessId, branchId = employee.BranchId });
            }

            ViewBag.Business = business;
            ViewBag.BusinessId = employee.BusinessId;
            var branches = await _context.Branches.Where(b => b.BusinessId == employee.BusinessId).ToListAsync();
            ViewBag.Branches = branches;
            return View("Employees", await _context.Employees.Where(e => e.BusinessId == employee.BusinessId).ToListAsync());
        }

        // POST: BusinessManagement/Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(int id, Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var existingEmployee = await _context.Employees
                .Include(e => e.Business)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existingEmployee == null || existingEmployee.Business == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && existingEmployee.Business.OwnerId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    existingEmployee.FirstName = employee.FirstName;
                    existingEmployee.LastName = employee.LastName;
                    existingEmployee.Phone = employee.Phone;
                    existingEmployee.Email = employee.Email;
                    existingEmployee.Position = employee.Position;
                    existingEmployee.Department = employee.Department;
                    existingEmployee.HireDate = employee.HireDate;
                    existingEmployee.Salary = employee.Salary;
                    existingEmployee.Status = employee.Status;
                    existingEmployee.IsManager = employee.IsManager;
                    existingEmployee.BranchId = employee.BranchId;
                    existingEmployee.Notes = employee.Notes;
                    existingEmployee.UpdatedDate = DateTime.Now;

                    _context.Update(existingEmployee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Employees), new { businessId = existingEmployee.BusinessId, branchId = existingEmployee.BranchId });
            }

            ViewBag.Business = existingEmployee.Business;
            ViewBag.BusinessId = existingEmployee.BusinessId;
            var branches = await _context.Branches.Where(b => b.BusinessId == existingEmployee.BusinessId).ToListAsync();
            ViewBag.Branches = branches;
            return View("Employees", await _context.Employees.Where(e => e.BusinessId == existingEmployee.BusinessId).ToListAsync());
        }

        // POST: BusinessManagement/Employees/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Business)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!User.IsInRole("Admin") && employee.Business?.OwnerId != userId)
            {
                return Forbid();
            }

            var businessId = employee.BusinessId;
            var branchId = employee.BranchId;
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Employees), new { businessId, branchId });
        }

        private bool BranchExists(int id)
        {
            return _context.Branches.Any(e => e.Id == id);
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
}
