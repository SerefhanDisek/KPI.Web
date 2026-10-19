using KpiEvaluation.Application;
using KpiEvaluation.Application.Repositories;
using KpiEvaluation.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KpiEvaluation.Infrastructure.Repositories;

public sealed class DevelopmentRepository(KpiDbContext db) : IDevelopmentRepository
{
    public async Task<CompetencyMatrixDto> GetMatrixAsync(CancellationToken ct)
    {
        var rows=await db.CompetencyMatrixEntries.AsNoTracking().OrderBy(x=>x.Profession.Name).ThenBy(x=>x.Seniority).ThenBy(x=>x.Competency).ThenBy(x=>x.Id)
            .Select(x=>new CompetencyMatrixEntryDto(x.Id,x.ProfessionId,x.Profession.Name,x.Seniority,x.Competency,x.ExpectedLevel,x.IsActive,x.Version)).ToListAsync(ct);
        var professions=await db.Professions.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new LookupDto(x.Id,x.Name)).ToListAsync(ct);
        return new(rows,professions);
    }

    public async Task<CompetencyMatrixEntryDto> SaveMatrixAsync(CompetencyMatrixSaveInput input, CancellationToken ct)
    {
        var seniority=input.Seniority.Trim();var competency=input.Competency.Trim();var expected=input.ExpectedLevel.Trim();
        if(seniority.Length is <1 or >100 || competency.Length is <1 or >200 || expected.Length is <1 or >500)
            throw new ValidationException("Kıdem, yetkinlik ve beklenen düzey zorunludur; uzunluk sınırlarını kontrol edin.");
        var profession=await db.Professions.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==input.ProfessionId,ct)??throw new ValidationException("Meslek bulunamadı.");
        if(await db.CompetencyMatrixEntries.AnyAsync(x=>x.Id!=input.Id&&x.ProfessionId==input.ProfessionId&&x.Seniority==seniority&&x.Competency==competency,ct))
            throw new ValidationException("Bu meslek, kıdem ve yetkinlik için zaten bir beklenti tanımlı.");
        CompetencyMatrixEntry entry;
        if(input.Id is {} id)
        {
            entry=await db.CompetencyMatrixEntries.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new ValidationException("Matris kaydı bulunamadı.");
            EnsureVersion(entry.Version,input.ExpectedVersion);entry.Version++;
        }
        else{entry=new CompetencyMatrixEntry{Seniority=seniority,Competency=competency,ExpectedLevel=expected};db.CompetencyMatrixEntries.Add(entry);}
        entry.ProfessionId=profession.Id;entry.Seniority=seniority;entry.Competency=competency;entry.ExpectedLevel=expected;entry.IsActive=input.IsActive;
        await SaveChangesAsync(ct);
        return new(entry.Id,entry.ProfessionId,profession.Name,entry.Seniority,entry.Competency,entry.ExpectedLevel,entry.IsActive,entry.Version);
    }

    public async Task<DevelopmentBoardDto> GetBoardAsync(int? employeeId, bool activeOnly, int page, int pageSize, int userId, CancellationToken ct)
    {
        if(page is <1 or >1_000_000 || pageSize is <1 or >100)throw new ValidationException("Sayfa ve sayfa boyutu geçerli değil.");
        var employees=await EmployeeScope(userId).OrderBy(x=>x.FullName).Select(x=>new LookupDto(x.Id,x.FullName)).ToListAsync(ct);
        var allowedIds=employees.Select(x=>x.Id).ToArray();
        var query=db.DevelopmentItems.AsNoTracking().Where(x=>allowedIds.Contains(x.EmployeeId)&&(!activeOnly||x.IsActive)&&(employeeId==null||x.EmployeeId==employeeId));
        var count=await query.CountAsync(ct);
        var rows=await query.OrderBy(x=>x.TargetDate==null).ThenBy(x=>x.TargetDate).ThenBy(x=>x.Id).Skip((page-1)*pageSize).Take(pageSize)
            .Select(x=>new DevelopmentItemDto(x.Id,x.EmployeeId,x.Employee.FullName,x.Kind,x.Title,x.Description,x.TargetDate,x.Progress,x.IsActive,x.Version,x.CreatedAt,x.UpdatedAt)).ToListAsync(ct);
        return new(rows,employees,count,page,pageSize);
    }

    public async Task<DevelopmentItemDto> SaveItemAsync(DevelopmentItemSaveInput input, int userId, CancellationToken ct)
    {
        if(!await EmployeeScope(userId).AnyAsync(x=>x.Id==input.EmployeeId,ct))throw new AuthorizationException();
        var title=input.Title.Trim();var description=input.Description?.Trim();
        if(title.Length is <1 or >200 || description?.Length>2000 || !Enum.IsDefined(input.Kind))throw new ValidationException("Başlık, açıklama veya kayıt türü geçerli değil.");
        ValidateProgress(input.Progress);
        DevelopmentItem entry;
        if(input.Id is {} id)
        {
            entry=await db.DevelopmentItems.SingleOrDefaultAsync(x=>x.Id==id,ct)??throw new ValidationException("Gelişim kaydı bulunamadı.");
            if(entry.EmployeeId!=input.EmployeeId)throw new ValidationException("Gelişim kaydının çalışanı değiştirilemez.");
            EnsureVersion(entry.Version,input.ExpectedVersion);entry.Version++;entry.UpdatedAt=DateTime.UtcNow;
        }
        else{entry=new DevelopmentItem{EmployeeId=input.EmployeeId,Title=title,CreatedByUserId=userId};db.DevelopmentItems.Add(entry);}
        entry.Kind=input.Kind;entry.Title=title;entry.Description=description;entry.TargetDate=input.TargetDate;entry.Progress=input.Progress;entry.IsActive=input.IsActive;
        await SaveChangesAsync(ct);
        var name=await db.Employees.Where(x=>x.Id==entry.EmployeeId).Select(x=>x.FullName).SingleAsync(ct);
        return new(entry.Id,entry.EmployeeId,name,entry.Kind,entry.Title,entry.Description,entry.TargetDate,entry.Progress,entry.IsActive,entry.Version,entry.CreatedAt,entry.UpdatedAt);
    }

    public async Task<IReadOnlyList<DevelopmentCheckInDto>> GetCheckInsAsync(long id, int userId, CancellationToken ct)
    {
        var scope=EmployeeScope(userId).Select(x=>x.Id);
        if(!await db.DevelopmentItems.AnyAsync(x=>x.Id==id&&scope.Contains(x.EmployeeId),ct))throw new AuthorizationException();
        return await db.DevelopmentCheckIns.AsNoTracking().Where(x=>x.DevelopmentItemId==id).OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.Id)
            .Select(x=>new DevelopmentCheckInDto(x.Id,x.Note,x.Progress,x.CreatedByUser.DisplayName,x.CreatedAt)).ToListAsync(ct);
    }

    public async Task AddCheckInAsync(long id, string note, decimal progress, int expectedVersion, int userId, CancellationToken ct)
    {
        note=note.Trim();if(note.Length is <1 or >2000)throw new ValidationException("Görüşme notu 1–2000 karakter olmalıdır.");ValidateProgress(progress);
        var scope=EmployeeScope(userId).Select(x=>x.Id);
        var entry=await db.DevelopmentItems.SingleOrDefaultAsync(x=>x.Id==id&&scope.Contains(x.EmployeeId),ct)??throw new AuthorizationException();
        if(!entry.IsActive)throw new ValidationException("Pasif kayda görüşme eklenemez.");
        EnsureVersion(entry.Version,expectedVersion);entry.Version++;entry.Progress=progress;entry.UpdatedAt=DateTime.UtcNow;
        entry.CheckIns.Add(new DevelopmentCheckIn{Note=note,Progress=progress,CreatedByUserId=userId});
        await SaveChangesAsync(ct);
    }

    private IQueryable<Employee> EmployeeScope(int userId)
    {
        var user=db.AppUsers.AsNoTracking().Where(x=>x.Id==userId&&x.IsActive&&x.Role.IsActive).Select(x=>new{x.Role.Code,x.EmployeeId}).SingleOrDefault();
        return user?.Code switch
        {
            RoleCodes.Administrator=>db.Employees,
            RoleCodes.FirstManager=>db.Employees.Where(x=>x.FirstManagerUserId==userId),
            RoleCodes.SecondManager=>db.Employees.Where(x=>x.SecondManagerUserId==userId),
            RoleCodes.Employee=>db.Employees.Where(x=>x.Id==user.EmployeeId),
            _=>db.Employees.Where(_=>false)
        };
    }

    private static void EnsureVersion(int current,int? expected){if(current!=expected)throw new ValidationException("Kayıt başka bir işlemle güncellendi. Güncel kaydı yeniden açın.");}
    private static void ValidateProgress(decimal progress){if(progress<0||progress>100)throw new ValidationException("İlerleme 0–100 arasında olmalıdır.");}
    private async Task SaveChangesAsync(CancellationToken ct)
    {
        try{await db.SaveChangesAsync(ct);}
        catch(DbUpdateConcurrencyException){throw new ValidationException("Kayıt başka bir işlemle güncellendi. Güncel kaydı yeniden açın.");}
        catch(DbUpdateException ex) when(ex.InnerException is SqlException{Number:2601 or 2627}){throw new ValidationException("Aynı matris tanımı başka bir işlemle oluşturuldu. Listeyi yenileyin.");}
    }
}
