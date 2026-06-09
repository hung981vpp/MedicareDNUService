namespace PharmacyBillingService.Security
{
    public static class RoleConstants
    {
        public const string Admin = "Admin";
        public const string Doctor = "Doctor";
        public const string Nurse = "Nurse";
        public const string Pharmacist = "Pharmacist";
        public const string Patient = "Patient";

        public const string DoctorOrStaff = Doctor + "," + Admin + "," + Nurse + "," + Pharmacist;
        public const string AdminOrNurse = Admin + "," + Nurse;
        public const string AdminOrPharmacist = Admin + "," + Pharmacist;
        public const string InventoryManagers = Admin + "," + Nurse + "," + Pharmacist;
        public const string Staff = Admin + "," + Nurse + "," + Pharmacist;
        public const string StaffOrPatient = Staff + "," + Patient;
    }
}
