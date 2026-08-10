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
    public class CutoffDatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CutoffDatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CutoffDates
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.CutoffDates.Include(c => c.AcademicPeriod);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: CutoffDates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cutoffDate = await _context.CutoffDates
                .Include(c => c.AcademicPeriod)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cutoffDate == null)
            {
                return NotFound();
            }

            return View(cutoffDate);
        }

        // GET: CutoffDates/Create
        public IActionResult Create()
        {
            ViewData["AcademicPeriodId"] = new SelectList(_context.AcademicPeriods, "Id", "Name");
            return View();
        }

        // POST: CutoffDates/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AcademicPeriodId,PhaseNumber,Name,StartDate,DueDate")] CutoffDate cutoffDate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cutoffDate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AcademicPeriodId"] = new SelectList(_context.AcademicPeriods, "Id", "Name", cutoffDate.AcademicPeriodId);
            return View(cutoffDate);
        }

        // GET: CutoffDates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cutoffDate = await _context.CutoffDates.FindAsync(id);
            if (cutoffDate == null)
            {
                return NotFound();
            }
            ViewData["AcademicPeriodId"] = new SelectList(_context.AcademicPeriods, "Id", "Name", cutoffDate.AcademicPeriodId);
            return View(cutoffDate);
        }

        // POST: CutoffDates/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AcademicPeriodId,PhaseNumber,Name,StartDate,DueDate")] CutoffDate cutoffDate)
        {
            if (id != cutoffDate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cutoffDate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CutoffDateExists(cutoffDate.Id))
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
            ViewData["AcademicPeriodId"] = new SelectList(_context.AcademicPeriods, "Id", "Name", cutoffDate.AcademicPeriodId);
            return View(cutoffDate);
        }

        // GET: CutoffDates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cutoffDate = await _context.CutoffDates
                .Include(c => c.AcademicPeriod)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (cutoffDate == null)
            {
                return NotFound();
            }

            return View(cutoffDate);
        }

        // POST: CutoffDates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cutoffDate = await _context.CutoffDates.FindAsync(id);
            if (cutoffDate != null)
            {
                _context.CutoffDates.Remove(cutoffDate);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CutoffDateExists(int id)
        {
            return _context.CutoffDates.Any(e => e.Id == id);
        }
    }
}
