namespace ClinicSystem.Domain.Enums;

public enum UserRole
{
    Admin = 1,       // 系统管理员
    Doctor = 2,      // 医生
    Nurse = 3,       // 护士
    Cashier = 4,     // 收银员
    Pharmacist = 5,  // 药剂师
    Boss = 6         // 老板/院长（只看数据）
}
