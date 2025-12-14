dotnet watch run
dotnet run

# Hướng dẫn quy trình git làm việc với team

## 1. Lấy code từ nhánh `developer`
Trước khi bắt đầu làm việc, hãy đảm bảo bạn đã cập nhật code mới nhất từ nhánh `developer`:
```bash
git checkout developer
git pull origin developer
```

## 2. Tạo nhánh mới
Tạo một nhánh mới với tên theo định dạng `tenban-cong-viec-thuc-hien`:
```bash
git checkout -b <tenban-cong-viec-thuc-hien>
```
Ví dụ, nếu bạn đang thực hiện tính năng đăng nhập, tên nhánh có thể là `linh-feature-login`:
```bash
git checkout -b linh-feature-login
```

## 3. Commit thay đổi
Sau khi hoàn thành công việc, commit các thay đổi của bạn:
```bash
git add .
git commit -m "Mô tả ngắn gọn về thay đổi"
```
Ví dụ:
```bash
git commit -m "Thêm tính năng đăng nhập người dùng"
```

## 4. Push nhánh lên remote repository
Push nhánh mới lên remote repository:
```bash
git push -u origin <tenban-cong-viec-thuc-hien>
```
Ví dụ:
```bash
git push -u origin linh-feature-login
```

## Lưu ý
- Luôn đảm bảo bạn đã cập nhật code mới nhất từ nhánh `developer` trước khi bắt đầu làm việc.
- Đặt tên nhánh rõ ràng và dễ hiểu để dễ dàng nhận biết.
- Commit khi xong 1 tính năg