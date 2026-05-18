using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取今日工作台统计数据
    /// </summary>
    /// <returns>今日挂号数、就诊数、收入、待诊数</returns>
    [HttpGet("today-stats")]
    public async Task<ActionResult> GetTodayStats()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayAppointments = await _db.Appointments
            .CountAsync(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow);

        var todayVisits = await _db.MedicalRecords
            .CountAsync(m => m.VisitDate >= today && m.VisitDate < tomorrow);

        var todayIncome = await _db.Payments
            .Where(p => p.CreatedAt >= today && p.CreatedAt < tomorrow && p.Status == "已支付")
            .SumAsync(p => p.ActualAmount);

        var waitingPatients = await _db.Appointments
            .CountAsync(a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow && a.Status == Domain.Enums.AppointmentStatus.Pending);

        return Ok(new
        {
            todayAppointments,
            todayVisits,
            todayIncome,
            waitingPatients
        });
    }
}
