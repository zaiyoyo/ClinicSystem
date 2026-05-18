using ClinicSystem.Domain.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SchedulesController(AppDbContext db) => _db = db;

    /// <summary>
    /// 获取某医生的常规排班
    /// </summary>
    [HttpGet("doctors/{doctorId}")]
    public async Task<ActionResult> GetDoctorSchedules(int doctorId)
    {
        var schedules = await _db.DoctorSchedules
            .Where(s => s.DoctorId == doctorId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
        return Ok(schedules);
    }

    /// <summary>
    /// 批量保存某医生的常规排班
    /// </summary>
    [HttpPost("doctors/{doctorId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SaveDoctorSchedules(int doctorId, [FromBody] List<DoctorSchedule> schedules)
    {
        var existing = await _db.DoctorSchedules.Where(s => s.DoctorId == doctorId).ToListAsync();
        _db.DoctorSchedules.RemoveRange(existing);

        foreach (var s in schedules)
        {
            s.Id = 0;
            s.DoctorId = doctorId;
            s.CreatedAt = DateTime.UtcNow;
        }
        _db.DoctorSchedules.AddRange(schedules);
        await _db.SaveChangesAsync();
        return Ok(new { message = "排班保存成功" });
    }

    /// <summary>
    /// 获取某医生的特殊排班（指定日期范围）
    /// </summary>
    [HttpGet("special/doctors/{doctorId}")]
    public async Task<ActionResult> GetSpecialSchedules(int doctorId, [FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var query = _db.SpecialSchedules.Where(s => s.DoctorId == doctorId);

        if (start.HasValue) query = query.Where(s => s.Date >= start.Value);
        if (end.HasValue) query = query.Where(s => s.Date <= end.Value);

        var items = await query.OrderBy(s => s.Date).ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// 批量保存特殊排班
    /// </summary>
    [HttpPost("special/doctors/{doctorId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SaveSpecialSchedules(int doctorId, [FromBody] List<SpecialSchedule> schedules)
    {
        foreach (var s in schedules)
        {
            var exist = await _db.SpecialSchedules
                .FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.Date == s.Date);

            if (exist != null)
            {
                exist.IsWorkDay = s.IsWorkDay;
                exist.TimeSlot = s.TimeSlot;
                exist.StartTime = s.StartTime;
                exist.EndTime = s.EndTime;
                exist.MaxPatients = s.MaxPatients;
                exist.Reason = s.Reason;
            }
            else
            {
                s.Id = 0;
                s.DoctorId = doctorId;
                s.CreatedAt = DateTime.UtcNow;
                _db.SpecialSchedules.Add(s);
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "特殊排班保存成功" });
    }

    /// <summary>
    /// 获取某日可挂号的医生列表（含排班信息和剩余号源）
    /// </summary>
    [HttpGet("available")]
    public async Task<ActionResult> GetAvailableDoctors([FromQuery] DateTime date)
    {
        var dayOfWeek = date.DayOfWeek;
        var dateOnly = date.Date;

        var doctors = await _db.Users
            .Where(u => u.Role == Domain.Enums.UserRole.Doctor && u.IsActive)
            .Include(u => u.Department)
            .ToListAsync();

        var result = new List<object>();

        foreach (var doc in doctors)
        {
            var specialSchedule = await _db.SpecialSchedules
                .FirstOrDefaultAsync(s => s.DoctorId == doc.Id && s.Date == dateOnly);

            if (specialSchedule != null && !specialSchedule.IsWorkDay)
                continue;

            var regularSchedules = await _db.DoctorSchedules
                .Where(s => s.DoctorId == doc.Id && s.DayOfWeek == dayOfWeek && s.IsActive)
                .ToListAsync();

            if (specialSchedule?.IsWorkDay == true)
            {
                result.Add(new
                {
                    doctorId = doc.Id,
                    doctorName = doc.DisplayName,
                    title = doc.Title,
                    departmentId = doc.DepartmentId,
                    departmentName = doc.Department?.Name,
                    consultationFee = doc.ConsultationFee ?? 0,
                    timeSlot = specialSchedule.TimeSlot ?? "上午",
                    startTime = specialSchedule.StartTime,
                    endTime = specialSchedule.EndTime,
                    maxPatients = specialSchedule.MaxPatients ?? doc.MaxDailyPatients ?? 30,
                    isSpecial = true
                });
            }
            else
            {
                foreach (var rs in regularSchedules)
                {
                    var bookedCount = await _db.Appointments
                        .CountAsync(a => a.DoctorId == doc.Id
                            && a.AppointmentDate == dateOnly
                            && a.TimeSlot == rs.TimeSlot
                            && a.Status != Domain.Enums.AppointmentStatus.Cancelled);

                    result.Add(new
                    {
                        doctorId = doc.Id,
                        doctorName = doc.DisplayName,
                        title = doc.Title,
                        departmentId = doc.DepartmentId,
                        departmentName = doc.Department?.Name,
                        consultationFee = doc.ConsultationFee ?? 0,
                        timeSlot = rs.TimeSlot,
                        startTime = rs.StartTime,
                        endTime = rs.EndTime,
                        maxPatients = rs.MaxPatients,
                        bookedCount = bookedCount,
                        remainingSlots = Math.Max(0, rs.MaxPatients - bookedCount),
                        isSpecial = false
                    });
                }
            }
        }

        return Ok(result);
    }
}
