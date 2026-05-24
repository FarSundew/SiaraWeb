using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    public class AcademicTrackingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AcademicTrackingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AcademicTrackings
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.AcademicTrackings.Include(a => a.Subject);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: AcademicTrackings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var academicTracking = await _context.AcademicTrackings
                .Include(a => a.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (academicTracking == null)
            {
                return NotFound();
            }

            return View(academicTracking);
        }

        // GET: AcademicTrackings/Create
        public IActionResult Create()
        {
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Id");
            return View();
        }

        // POST: AcademicTrackings/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SubjectId,UnitNumber,ApprovalPercentage,FailurePercentage,DropoutPercentage,Phase")] AcademicTracking academicTracking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(academicTracking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Id", academicTracking.SubjectId);
            return View(academicTracking);
        }

        // GET: AcademicTrackings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var academicTracking = await _context.AcademicTrackings.FindAsync(id);
            if (academicTracking == null)
            {
                return NotFound();
            }
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Id", academicTracking.SubjectId);
            return View(academicTracking);
        }

        // POST: AcademicTrackings/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SubjectId,UnitNumber,ApprovalPercentage,FailurePercentage,DropoutPercentage,Phase")] AcademicTracking academicTracking)
        {
            if (id != academicTracking.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(academicTracking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AcademicTrackingExists(academicTracking.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["SubjectId"] = new SelectList(_context.Subjects, "Id", "Id", academicTracking.SubjectId);
            return View(academicTracking);
        }

        // GET: AcademicTrackings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var academicTracking = await _context.AcademicTrackings
                .Include(a => a.Subject)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (academicTracking == null)
            {
                return NotFound();
            }

            return View(academicTracking);
        }

        // POST: AcademicTrackings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var academicTracking = await _context.AcademicTrackings.FindAsync(id);
            if (academicTracking != null)
            {
                _context.AcademicTrackings.Remove(academicTracking);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AcademicTrackingExists(int id)
        {
            return _context.AcademicTrackings.Any(e => e.Id == id);
        }
    }
}
