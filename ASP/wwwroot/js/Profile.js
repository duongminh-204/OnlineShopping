function openProfile() {
    fetch('/Account/MyProfile')
        .then(res => res.text())
        .then(html => {

            const profileContent = document.getElementById("profileContent");
            profileContent.innerHTML = html;

            const modalHeader = document.querySelector('#profileModal .modal-header');
            const existingEditBtns = modalHeader.querySelectorAll('.btn-edit-profile');
            existingEditBtns.forEach(btn => btn.remove());

            const editBtn = document.createElement('button');
            editBtn.className = 'btn btn-sm btn-primary ms-2 btn-edit-profile';
            editBtn.innerHTML = '<i class="bi bi-pencil-square"></i> Chỉnh sửa';
            editBtn.onclick = function () {
                fetch('/Account/EditProfile')
                    .then(res => res.text())
                    .then(editHtml => {
                        profileContent.innerHTML = editHtml;


                        const form = document.getElementById('editProfileForm');
                        if (form) {
                            form.addEventListener('submit', function (e) {
                                e.preventDefault();
                                const formData = new FormData(this);

                                fetch('/Account/UpdateProfile', {
                                    method: 'POST',
                                    body: formData
                                })
                                    .then(res => {
                                        if (!res.ok) throw new Error('Cập nhật thất bại');
                                        return res.text();
                                    })
                                    .then(updatedHtml => {
                                        profileContent.innerHTML = updatedHtml;
                                        alert('Cập nhật thông tin thành công!');

                                        setTimeout(() => openProfile(), 1500);
                                    })
                                    .catch(err => {
                                        console.error(err);
                                        alert('Có lỗi xảy ra khi cập nhật.');
                                    });
                            });
                        }
                    })
                    .catch(err => console.error('Lỗi load EditProfile:', err));
            };


            if (modalHeader) {
                modalHeader.appendChild(editBtn);
            }

            const modal = new bootstrap.Modal(document.getElementById('profileModal'));
            modal.show();
        })
        .catch(err => console.error('Lỗi load MyProfile:', err));
}