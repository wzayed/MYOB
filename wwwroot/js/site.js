(() => {
    const today = new Date();
    const maximumDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

    document.querySelectorAll('input[type="date"]').forEach(input => {
        input.max = maximumDate;
    });
})();
document.querySelectorAll('[data-copy-payment-notes]').forEach(button => {
    button.addEventListener('click', () => {
        const form = button.closest('form');
        const source = form?.querySelector('[name="Item.Comments"]');
        const target = form?.querySelector('[name="Item.PublicComments"]');
        if (!source || !target) return;
        target.value = source.value;
        target.dispatchEvent(new Event('input', { bubbles: true }));
        target.dispatchEvent(new Event('change', { bubbles: true }));
        target.focus();
    });
});
