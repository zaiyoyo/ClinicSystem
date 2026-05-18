using ClinicSystem.Domain.Entities;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AppointmentsController(AppDbContext db) => _db = db;

    /// <summary>
    /// 挂号列表（分页+筛选）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] DateTime? date,
        [FromQuery] int? doctorId,
        [FromQuery] int? departmentId,
        [FromQuery] string? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .AsQueryable();

        if (date.HasValue)
        {
            var d = date.Value.Date;
            var next = d.AddDays(1);
            query = query.Where(a => a.AppointmentDate >= d && a.AppointmentDate < next);
        }
        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);
        if (departmentId.HasValue)
            query = query.Where(a => a.DepartmentId == departmentId.Value);
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<AppointmentStatus>(status, out var s))
                query = query.Where(a => a.Status == s);
        }
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(a => a.Patient.Name.Contains(keyword) || a.AppointmentNo.Contains(keyword));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id, a.AppointmentNo, a.AppointmentDate, a.TimeSlot, a.QueueNumber,
                Status = a.Status.ToString(), a.Source, a.Remark, a.CreatedAt,
                PatientId = a.PatientId, PatientName = a.Patient.Name, PatientPhone = a.Patient.Phone, PatientGender = a.Patient.Gender,
                DoctorId = a.DoctorId, DoctorName = a.Doctor.DisplayName, DoctorTitle = a.Doctor.Title,
                DepartmentId = a.DepartmentId, DepartmentName = a.Department!.Name
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// 获取单个挂号详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var apt = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (apt == null) return NotFound();
        return Ok(apt);
    }

    /// <summary>
    /// 现场挂号 / 预约挂号
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Nurse,Cashier")]
    public async Task<ActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        var doctor = await _db.Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == request.DoctorId && u.Role == UserRole.Doctor && u.IsActive);
        if (doctor == null) return BadRequest(new { message = "医生不存在或已停诊" });

        var today = DateTime.UtcNow.Date;
        var apptDate = request.AppointmentDate.Date;
        if (apptDate < today) return BadRequest(new { message = "挂号日期不能早于今天" });

        var maxPatients = doctor.MaxDailyPatients ?? 30;
        var existingCount = await _db.Appointments.CountAsync(a =>
            a.DoctorId == request.DoctorId && a.AppointmentDate == apptDate && a.TimeSlot == request.TimeSlot && a.Status != AppointmentStatus.Cancelled);
        if (existingCount >= maxPatients)
            return BadRequest(new { message = "该时段号源已满" });

        var queueNumber = existingCount + 1;
        var appointmentNo = $"GH{apptDate:yyyyMMdd}{request.DoctorId:D3}{queueNumber:D3}";

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var apt = new Appointment
        {
            AppointmentNo = appointmentNo,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            DepartmentId = doctor.DepartmentId,
            RegisterUserId = userId,
            AppointmentDate = apptDate,
            TimeSlot = request.TimeSlot,
            QueueNumber = queueNumber,
            Status = AppointmentStatus.Pending,
            Source = request.Source ?? "现场",
            Remark = request.Remark
        };

        _db.Appointments.Add(apt);
        await _db.SaveChangesAsync();

        apt = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .FirstAsync(a => a.Id == apt.Id);

        return CreatedAtAction(nameof(GetById), new { id = apt.Id }, apt);
    }

    /// <summary>
    /// 改签（更换医生或日期时段）
    /// </summary>
    [HttpPut("{id}/reschedule")]
    [Authorize(Roles = "Admin,Nurse,Cashier")]
    public async Task<ActionResult> Reschedule(int id, [FromBody] RescheduleRequest request)
    {
        var apt = await _db.Appointments.FindAsync(id);
        if (apt == null) return NotFound();
        if (apt.Status == AppointmentStatus.Cancelled || apt.Status == AppointmentStatus.Completed)
            return BadRequest(new { message = "当前状态不支持改签" });

        var doctor = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.DoctorId && u.Role == UserRole.Doctor);
        if (doctor == null) return BadRequest(new { message = "医生不存在" });

        var newDate = request.AppointmentDate.Date;
        var existingCount = await _db.Appointments.CountAsync(a =>
            a.DoctorId == request.DoctorId && a.AppointmentDate == newDate && a.TimeSlot == request.TimeSlot && a.Id != id && a.Status != AppointmentStatus.Cancelled);
        var maxPatients = doctor.MaxDailyPatients ?? 30;
        if (existingCount >= maxPatients)
            return BadRequest(new { message = "该时段号源已满" });

        apt.DoctorId = request.DoctorId;
        apt.AppointmentDate = newDate;
        apt.TimeSlot = request.TimeSlot;
        apt.QueueNumber = existingCount + 1;
        apt.Remark = (apt.Remark ?? "") + " [已改签]";
        await _db.SaveChangesAsync();
        return Ok(new { message = "改签成功" });
    }

    /// <summary>
    /// 退号/取消挂号
    /// </summary>
    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "Admin,Nurse,Cashier")]
    public async Task<ActionResult> Cancel(int id, [FromBody] CancelRequest? request)
    {
        var apt = await _db.Appointments.FindAsync(id);
        if (apt == null) return NotFound();
        if (apt.Status == AppointmentStatus.Cancelled || apt.Status == AppointmentStatus.Completed)
            return BadRequest(new { message = "当前状态不支持退号" });

        apt.Status = AppointmentStatus.Cancelled;
        apt.CancelledAt = DateTime.UtcNow;
        apt.CancelReason = request?.Reason ?? "用户退号";
        await _db.SaveChangesAsync();
        return Ok(new { message = "退号成功" });
    }

    /// <summary>
    /// 开始就诊（叫号/接诊）
    /// </summary>
    [HttpPut("{id}/start-visit")]
    [Authorize(Roles = "Admin,Doctor,Nurse")]
    public async Task<ActionResult> StartVisit(int id)
    {
        var apt = await _db.Appointments.FindAsync(id);
        if (apt == null) return NotFound();
        if (apt.Status != AppointmentStatus.Pending)
            return BadRequest(new { message = "当前状态不能开始就诊" });

        apt.Status = AppointmentStatus.InProgress;
        await _db.SaveChangesAsync();
        return Ok(new { message = "已叫号" });
    }

    /// <summary>
    /// 完成就诊
    /// </summary>
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<ActionResult> Complete(int id)
    {
        var apt = await _db.Appointments.FindAsync(id);
        if (apt == null) return NotFound();

        apt.Status = AppointmentStatus.Completed;
        await _db.SaveChangesAsync();
        return Ok(new { message = "就诊完成" });
    }

    /// <summary>
    /// 获取叫号队列（当日指定医生的排队列表）
    /// </summary>
    [HttpGet("queue")]
    public async Task<ActionResult> GetQueue([FromQuery] DateTime? date, [FromQuery] int? doctorId, [FromQuery] int? departmentId)
    {
        var queryDate = date?.Date ?? DateTime.UtcNow.Date;
        var nextDay = queryDate.AddDays(1);

        var query = _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Department)
            .Where(a => a.AppointmentDate >= queryDate && a.AppointmentDate < nextDay)
            .AsQueryable();

        if (doctorId.HasValue) query = query.Where(a => a.DoctorId == doctorId.Value);
        if (departmentId.HasValue) query = query.Where(a => a.DepartmentId == departmentId.Value);

        var items = await query
            .OrderBy(a => a.TimeSlot)
            .ThenBy(a => a.QueueNumber)
            .Select(a => new
            {
                a.Id, a.AppointmentNo, a.TimeSlot, a.QueueNumber,
                Status = a.Status.ToString(), PatientName = a.Patient.Name, DoctorName = a.Doctor.DisplayName,
                DepartmentName = a.Department!.Name
            })
            .ToListAsync();

        return Ok(items);
    }
}

public class CreateAppointmentRequest
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = "上午";
    public string? Source { get; set; }
    public string? Remark { get; set; }
}

public class RescheduleRequest
{
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
}

public class CancelRequest
{
    public string? Reason { get; set; }
}
