(() => {
    const today = new Date();
    const maximumDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

    document.querySelectorAll('input[type="date"]').forEach(input => {
        input.max = maximumDate;
    });
})();
