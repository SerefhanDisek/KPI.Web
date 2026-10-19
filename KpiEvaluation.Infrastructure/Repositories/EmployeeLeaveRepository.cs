using KpiEvaluation.Application;
using KpiEvaluation.Domain;
using Microsoft.EntityFrameworkCore;
using KpiEvaluation.Application.Repositories;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class EmployeeLeaveRepository(KpiDbContext db) : IEmployeeLeaveRepository
{
    public async Task<EmployeeLeavePageDto> GetAsync(int? employeeId, int? professionId, DateOnly? from, DateOnly? to,
        bool activeOnly, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new ValidationException("Sayfa geçerli bir aralıkta, sayfa boyutu 1–100 arasında olmalıdır.");
        if (from is not null && to is not null && to < from) throw new ValidationException("Bitiş tarihi başlangıçtan önce olamaz.");
        var query = db.EmployeeLeaves.AsNoTracking().Where(x => (!activeOnly || x.IsActive)
            && (employeeId == null || x.EmployeeId == employeeId)
            && (professionId == null || x.Employee.ProfessionId == professionId)
            && (from == null || x.EndDate >= from) && (to == null || x.StartDate <= to));
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.StartDate).ThenByDescending(x => x.Id)
            .Select(x => new EmployeeLeaveDto(x.Id, x.EmployeeId, x.Employee.FullName, x.Employee.ProfessionId,
                x.Employee.Profession.Name, x.StartDate, x.EndDate, x.LeaveType, x.Note, x.IsActive))
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items, count, page, pageSize);
    }

    public async Task<EmployeeLeaveDto> SaveAsync(EmployeeLeaveSaveInput input, int userId, CancellationToken ct = default)
    {
        var leaveType = input.LeaveType?.Trim();
        var note = input.Note?.Trim();
        if (string.IsNullOrWhiteSpace(leaveType) || leaveType.Length > 100) throw new ValidationException("İzin türü zorunludur ve en fazla 100 karakter olabilir.");
        if (note?.Length > 2000) throw new ValidationException("Açıklama en fazla 2000 karakter olabilir.");
        if (input.StartDate == default || input.EndDate == default || input.EndDate < input.StartDate)
            throw new ValidationException("Geçerli bir başlangıç ve bitiş tarihi seçmelisiniz.");
        var employee = await db.Employees.AsNoTracking().Where(x => x.Id == input.EmployeeId)
            .Select(x => new { x.Id, x.FullName, x.ProfessionId, ProfessionName = x.Profession.Name, x.IsActive }).SingleOrDefaultAsync(ct)
            ?? throw new ValidationException("Çalışan bulunamadı.");
        EmployeeLeave entity;
        if (input.Id is null)
        {
            if (!employee.IsActive) throw new ValidationException("Aktif bir çalışan seçmelisiniz.");
            entity = new EmployeeLeave { EmployeeId = employee.Id, LeaveType = leaveType, CreatedByUserId = userId };
            db.EmployeeLeaves.Add(entity);
        }
        else
        {
            entity = await db.EmployeeLeaves.SingleOrDefaultAsync(x => x.Id == input.Id, ct)
                ?? throw new ValidationException("İzin kaydı bulunamadı.");
            if (!employee.IsActive && entity.EmployeeId != employee.Id) throw new ValidationException("Aktif bir çalışan seçmelisiniz.");
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedByUserId = userId;
        }
        entity.EmployeeId = employee.Id;
        entity.StartDate = input.StartDate;
        entity.EndDate = input.EndDate;
        entity.LeaveType = leaveType;
        entity.Note = note;
        entity.IsActive = input.IsActive;
        await db.SaveChangesAsync(ct);
        return new(entity.Id, employee.Id, employee.FullName, employee.ProfessionId, employee.ProfessionName,
            entity.StartDate, entity.EndDate, entity.LeaveType, entity.Note, entity.IsActive);
    }
}
